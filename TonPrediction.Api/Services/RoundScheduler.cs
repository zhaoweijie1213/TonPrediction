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
        private readonly string[] _symbols = ["ton", "btc", "eth"];

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
                await _hub.Clients.All.SendAsync("roundEnded", new { roundId = locked.Epoch }, token);
            }

            // 获取当前可下注的回合
            var live = await roundRepo.GetCurrentLiveAsync(symbol, token);
            if (live == null)
            {
                var startPrice = (await priceService.GetAsync(symbol, "usd", token)).Price;
                var last = await roundRepo.GetLatestAsync(symbol, token);
                var firstRound = new RoundEntity
                {
                    Symbol = symbol,
                    Epoch = (last?.Epoch ?? 0) + 1,
                    StartTime = now,
                    LockTime = now.Add(_interval),
                    CloseTime = now.Add(_interval * 2),
                    LockPrice = startPrice,
                    Status = RoundStatus.Live
                };
                await roundRepo.InsertAsync(firstRound);
                await priceRepo.InsertAsync(new PriceSnapshotEntity { Symbol = symbol, Timestamp = now, Price = startPrice });
                await _hub.Clients.All.SendAsync("currentRound", new
                {
                    roundId = firstRound.Epoch,
                    lockPrice = firstRound.LockPrice.ToString("F8"),
                    currentPrice = firstRound.LockPrice.ToString("F8"),
                    totalAmount = firstRound.TotalAmount.ToString("F8"),
                    upAmount = firstRound.BullAmount.ToString("F8"),
                    downAmount = firstRound.BearAmount.ToString("F8"),
                    rewardPool = firstRound.RewardAmount.ToString("F8"),
                    endTime = new DateTimeOffset(firstRound.CloseTime).ToUnixTimeSeconds(),
                    oddsUp = "0",
                    oddsDown = "0",
                    status = firstRound.Status
                }, token);
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
                await _hub.Clients.All.SendAsync("roundLocked", new { roundId = live.Epoch }, token);

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
                await _hub.Clients.All.SendAsync("currentRound", new
                {
                    roundId = nextRound.Epoch,
                    lockPrice = nextRound.LockPrice.ToString("F8"),
                    currentPrice = nextRound.LockPrice.ToString("F8"),
                    totalAmount = nextRound.TotalAmount.ToString("F8"),
                    upAmount = nextRound.BullAmount.ToString("F8"),
                    downAmount = nextRound.BearAmount.ToString("F8"),
                    rewardPool = nextRound.RewardAmount.ToString("F8"),
                    endTime = new DateTimeOffset(nextRound.CloseTime).ToUnixTimeSeconds(),
                    oddsUp = "0",
                    oddsDown = "0",
                    status = nextRound.Status
                }, token);
            }
        }
    }
}
