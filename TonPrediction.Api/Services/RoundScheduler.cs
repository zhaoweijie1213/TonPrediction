using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Linq;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Enums;
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
        IPredictionHubService notifier,
        ILogger<RoundScheduler> logger,
        IDistributedLock locker) : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private readonly IPredictionHubService _notifier = notifier;
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
                        TimeSpan.FromSeconds(10));
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
            var locked = await roundRepo.GetCurrentLockedAsync(symbol);
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
                await _notifier.PushRoundEndedAsync(locked.Epoch);
                await _notifier.PushSettlementEndedAsync(locked.Epoch);
            }

            // 获取当前可下注的回合
            var live = await roundRepo.GetCurrentLiveAsync(symbol);
            if (live == null)
            {
                var startPrice = (await priceService.GetAsync(symbol, "usd")).Price;
                var last = await roundRepo.GetLatestAsync(symbol);

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
                    await _notifier.PushRoundLockedAsync(genesisLocked.Epoch);
                    await _notifier.PushSettlementStartedAsync(genesisLocked.Epoch);

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
                    await _notifier.PushCurrentRoundAsync(liveRound, liveRound.LockPrice);
                    await _notifier.PushRoundStartedAsync(liveRound.Epoch);
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
                    await _notifier.PushCurrentRoundAsync(firstRound, firstRound.LockPrice);
                    await _notifier.PushRoundStartedAsync(firstRound.Epoch);
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
                await _notifier.PushRoundLockedAsync(live.Epoch);
                await _notifier.PushSettlementStartedAsync(live.Epoch);

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
                await _notifier.PushCurrentRoundAsync(nextRound, nextRound.LockPrice);
                await _notifier.PushRoundStartedAsync(nextRound.Epoch);
            }
        }
    }
}
