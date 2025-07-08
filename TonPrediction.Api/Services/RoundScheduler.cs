using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Linq;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Enums;
using PancakeSwap.Api.Hubs;
using TonPrediction.Application.Services.Interface;
using TonPrediction.Application.Cache;
using TonPrediction.Application.Output;

namespace TonPrediction.Api.Services
{
    /// <summary>
    /// 定期创建和结束回合的后台服务，具备容错恢复能力。
    /// </summary>
    public class RoundScheduler(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        IHubContext<PredictionHub> hub,
        ILogger<RoundScheduler> logger,
        IDistributedLock locker) : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private readonly IHubContext<PredictionHub> _hub = hub;
        private readonly ILogger<RoundScheduler> _logger = logger;
        private readonly IDistributedLock _locker = locker;
        private readonly TimeSpan _interval =
            TimeSpan.FromSeconds(configuration.GetValue<int>("ENV_ROUND_INTERVAL_SEC", 300));
        private readonly string[] _symbols = configuration.GetSection("Symbols").Get<string[]>()!;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var tasks = _symbols.Select(s => Task.Run(() => RunSymbolLoopAsync(s, stoppingToken), stoppingToken)).ToArray();
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// 执行每个币种的回合循环
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task RunSymbolLoopAsync(string symbol, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using var handle = await _locker.AcquireAsync(
                        CacheKeyCollection.GetRoundSchedulerLockKey(symbol),
                        TimeSpan.FromSeconds(10),
                        token);
                    if (handle != null)
                    {
                        await HandleRoundAsync(symbol, token);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Round scheduler error for {Symbol}", symbol);
                }

                await Task.Delay(TimeSpan.FromSeconds(5), token);
            }
        }

        /// <summary>
        /// 回合处理
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task HandleRoundAsync(string symbol, CancellationToken token)
        {
            using var scope = _scopeFactory.CreateScope();
            var roundRepo = scope.ServiceProvider.GetRequiredService<IRoundRepository>();
            var priceRepo = scope.ServiceProvider.GetRequiredService<IPriceSnapshotRepository>();
            var priceService = scope.ServiceProvider.GetRequiredService<IPriceService>();
            var betRepo = scope.ServiceProvider.GetRequiredService<IBetRepository>();

            var now = DateTime.UtcNow;

            // 结束上一回合并结算奖励
            var locked = await roundRepo.GetCurrentLockedAsync(symbol, token);
            if (locked != null && locked.CloseTime <= now)
            {
                var closePrice = (await priceService.GetAsync(symbol, "usd", token)).Price;
                locked.CloseTime = now;
                locked.ClosePrice = closePrice;
                locked.Status = RoundStatus.Ended;

                Position? winner = null;
                if (closePrice > locked.LockPrice) winner = Position.Bull;
                else if (closePrice < locked.LockPrice) winner = Position.Bear;

                locked.WinnerSide = winner.HasValue ? (decimal)winner.Value : 0;
                locked.RewardBaseCalAmount = locked.TotalAmount;
                await roundRepo.UpdateByPrimaryKeyAsync(locked);

                var bets = await betRepo.GetByRoundAsync(locked.Id, token);
                var winTotal = winner switch
                {
                    Position.Bull => locked.BullAmount,
                    Position.Bear => locked.BearAmount,
                    _ => 0m
                };

                foreach (var bet in bets)
                {
                    decimal reward = 0m;
                    if (!winner.HasValue)
                    {
                        reward = bet.Amount;
                    }
                    else if ((winner == Position.Bull && bet.Position == Position.Bull) ||
                             (winner == Position.Bear && bet.Position == Position.Bear))
                    {
                        reward = winTotal > 0m ? bet.Amount / winTotal * locked.RewardAmount : 0m;
                    }
                    bet.Reward = reward;
                    await betRepo.UpdateByPrimaryKeyAsync(bet);
                }

                await priceRepo.InsertAsync(new PriceSnapshotEntity { Symbol = symbol, Timestamp = now, Price = closePrice });
                await _hub.Clients.All.SendAsync("roundEnded", new RoundEndedOutput
                {
                    RoundId = locked.Epoch
                }, token);
                await _hub.Clients.All.SendAsync("settlementEnded", new SettlementEndedOutput
                {
                    RoundId = locked.Epoch
                }, token);
            }

            // 获取当前可下注的回合
            var live = await roundRepo.GetCurrentLiveAsync(symbol, token);
            if (live == null)
            {
                var startPrice = (await priceService.GetAsync(symbol, "usd", token)).Price;
                var last = await roundRepo.GetLatestAsync(symbol, token);

                if (last == null)
                {
                    // 创世回合：立即锁定首轮并同步创建可下注的下一轮
                    var genesisLocked = new RoundEntity
                    {
                        Symbol = symbol,
                        Epoch = 1,
                        StartTime = now,
                        LockTime = now,
                        CloseTime = now.Add(_interval),
                        LockPrice = startPrice,
                        Status = RoundStatus.Locked
                    };
                    await roundRepo.InsertAsync(genesisLocked);
                    await priceRepo.InsertAsync(new PriceSnapshotEntity { Symbol = symbol, Timestamp = now, Price = startPrice });
                    await _hub.Clients.All.SendAsync("roundLocked", new RoundLockedOutput
                    {
                        RoundId = genesisLocked.Epoch
                    }, token);
                    await _hub.Clients.All.SendAsync("settlementStarted", new SettlementStartedOutput
                    {
                        RoundId = genesisLocked.Epoch
                    }, token);

                    var liveRound = new RoundEntity
                    {
                        Symbol = symbol,
                        Epoch = 2,
                        StartTime = now,
                        LockTime = now.Add(_interval),
                        CloseTime = now.Add(_interval * 2),
                        LockPrice = startPrice,
                        Status = RoundStatus.Live
                    };
                    await roundRepo.InsertAsync(liveRound);
                    await priceRepo.InsertAsync(new PriceSnapshotEntity { Symbol = symbol, Timestamp = now, Price = startPrice });
                    var oddsBull = liveRound.BullAmount > 0m
                        ? liveRound.TotalAmount / liveRound.BullAmount
                        : 0m;
                    var oddsBear = liveRound.BearAmount > 0m
                        ? liveRound.TotalAmount / liveRound.BearAmount
                        : 0m;
                    await _hub.Clients.All.SendAsync(
                        "currentRound",
                        new CurrentRoundOutput
                        {
                            RoundId = liveRound.Epoch,
                            LockPrice = liveRound.LockPrice.ToString("F8"),
                            CurrentPrice = liveRound.LockPrice.ToString("F8"),
                            TotalAmount = liveRound.TotalAmount.ToString("F8"),
                            BullAmount = liveRound.BullAmount.ToString("F8"),
                            BearAmount = liveRound.BearAmount.ToString("F8"),
                            RewardPool = liveRound.RewardAmount.ToString("F8"),
                            EndTime = new DateTimeOffset(liveRound.CloseTime).ToUnixTimeSeconds(),
                            BullOdds = oddsBull.ToString("F8"),
                            BearOdds = oddsBear.ToString("F8"),
                            Status = liveRound.Status
                        },
                        token);
                    await _hub.Clients.All.SendAsync("roundStarted", new RoundStartedOutput
                    {
                        RoundId = liveRound.Epoch
                    }, token);
                }
                else
                {
                    var firstRound = new RoundEntity
                    {
                        Symbol = symbol,
                        Epoch = (last.Epoch) + 1,
                        StartTime = now,
                        LockTime = now.Add(_interval),
                        CloseTime = now.Add(_interval * 2),
                        LockPrice = startPrice,
                        Status = RoundStatus.Live
                    };
                    await roundRepo.InsertAsync(firstRound);
                    await priceRepo.InsertAsync(new PriceSnapshotEntity { Symbol = symbol, Timestamp = now, Price = startPrice });
                    var oddsBull = firstRound.BullAmount > 0m
                        ? firstRound.TotalAmount / firstRound.BullAmount
                        : 0m;
                    var oddsBear = firstRound.BearAmount > 0m
                        ? firstRound.TotalAmount / firstRound.BearAmount
                        : 0m;
                    await _hub.Clients.All.SendAsync(
                        "currentRound",
                        new CurrentRoundOutput
                        {
                            RoundId = firstRound.Epoch,
                            LockPrice = firstRound.LockPrice.ToString("F8"),
                            CurrentPrice = firstRound.LockPrice.ToString("F8"),
                            TotalAmount = firstRound.TotalAmount.ToString("F8"),
                            BullAmount = firstRound.BullAmount.ToString("F8"),
                            BearAmount = firstRound.BearAmount.ToString("F8"),
                            RewardPool = firstRound.RewardAmount.ToString("F8"),
                            EndTime = new DateTimeOffset(firstRound.CloseTime).ToUnixTimeSeconds(),
                            BullOdds = oddsBull.ToString("F8"),
                            BearOdds = oddsBear.ToString("F8"),
                            Status = firstRound.Status
                        },
                        token);
                    await _hub.Clients.All.SendAsync("roundStarted", new RoundStartedOutput
                    {
                        RoundId = firstRound.Epoch
                    }, token);
                }

                return;
            }

            // 到达锁定时间时，锁定当前回合并创建下一回合
            if (live.LockTime <= now && live.Status == RoundStatus.Live)
            {
                var lockPrice = (await priceService.GetAsync(symbol, "usd", token)).Price;
                live.LockPrice = lockPrice;
                live.LockTime = now;
                live.CloseTime = now.Add(_interval);
                live.Status = RoundStatus.Locked;
                await roundRepo.UpdateByPrimaryKeyAsync(live);
                await priceRepo.InsertAsync(new PriceSnapshotEntity { Symbol = symbol, Timestamp = now, Price = lockPrice });
                await _hub.Clients.All.SendAsync("roundLocked", new RoundLockedOutput
                {
                    RoundId = live.Epoch
                }, token);
                await _hub.Clients.All.SendAsync("settlementStarted", new SettlementStartedOutput
                {
                    RoundId = live.Epoch
                }, token);

                var nextPrice = lockPrice;
                var nextRound = new RoundEntity
                {
                    Symbol = symbol,
                    Epoch = live.Epoch + 1,
                    StartTime = now,
                    LockTime = now.Add(_interval),
                    CloseTime = now.Add(_interval * 2),
                    LockPrice = nextPrice,
                    Status = RoundStatus.Live
                };
                await roundRepo.InsertAsync(nextRound);
                await priceRepo.InsertAsync(new PriceSnapshotEntity { Symbol = symbol, Timestamp = now, Price = nextPrice });
                var oddsBull = nextRound.BullAmount > 0m
                    ? nextRound.TotalAmount / nextRound.BullAmount
                    : 0m;
                var oddsBear = nextRound.BearAmount > 0m
                    ? nextRound.TotalAmount / nextRound.BearAmount
                    : 0m;
                await _hub.Clients.All.SendAsync(
                    "currentRound",
                    new CurrentRoundOutput
                    {
                        RoundId = nextRound.Epoch,
                        LockPrice = nextRound.LockPrice.ToString("F8"),
                        CurrentPrice = nextRound.LockPrice.ToString("F8"),
                        TotalAmount = nextRound.TotalAmount.ToString("F8"),
                        BullAmount = nextRound.BullAmount.ToString("F8"),
                        BearAmount = nextRound.BearAmount.ToString("F8"),
                        RewardPool = nextRound.RewardAmount.ToString("F8"),
                        EndTime = new DateTimeOffset(nextRound.CloseTime).ToUnixTimeSeconds(),
                        BullOdds = oddsBull.ToString("F8"),
                        BearOdds = oddsBear.ToString("F8"),
                        Status = nextRound.Status
                    },
                    token);
                await _hub.Clients.All.SendAsync("roundStarted", new RoundStartedOutput
                {
                    RoundId = nextRound.Epoch
                }, token);
            }
        }
    }
}
