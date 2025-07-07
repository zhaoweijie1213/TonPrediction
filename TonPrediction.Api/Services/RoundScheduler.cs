using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Enums;
using PancakeSwap.Api.Hubs;
using TonPrediction.Application.Services.Interface;

namespace TonPrediction.Api.Services
{
    /// <summary>
    /// 定期创建和结束回合的后台服务，具备容错恢复能力。
    /// </summary>
    public class RoundScheduler(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        IHubContext<PredictionHub> hub,
        ILogger<RoundScheduler> logger) : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private readonly IHubContext<PredictionHub> _hub = hub;
        private readonly ILogger<RoundScheduler> _logger = logger;
        private readonly TimeSpan _interval =
            TimeSpan.FromSeconds(configuration.GetValue<int>("ENV_ROUND_INTERVAL_SEC", 300));
        private readonly string[] _symbols = ["ton", "btc", "eth"];

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    foreach (var symbol in _symbols)
                    {
                        await HandleRoundAsync(symbol, CancellationToken.None);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Round scheduler error");
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        /// <summary>
        /// 回合处理逻辑
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

            var now = DateTime.UtcNow;

            // 结束已锁定的回合
            var locked = await roundRepo.GetCurrentLockedAsync(symbol, token);
            if (locked != null && locked.CloseTime <= now)
            {
                var closePrice = (await priceService.GetAsync(symbol, "usd", token)).Price;
                locked.CloseTime = now;
                locked.ClosePrice = closePrice;
                locked.Status = RoundStatus.Ended;
                await roundRepo.UpdateByPrimaryKeyAsync(locked);
                await priceRepo.InsertAsync(new PriceSnapshotEntity { Symbol = symbol, Timestamp = now, Price = closePrice });
                await _hub.Clients.All.SendAsync("roundEnded", new { roundId = locked.Id }, token);
            }

            // 获取当前可下注的回合
            var live = await roundRepo.GetCurrentLiveAsync(symbol, token);
            if (live == null)
            {
                var startPrice = (await priceService.GetAsync(symbol, "usd", token)).Price;
                var firstRound = new RoundEntity
                {
                    Symbol = symbol,
                    Id = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
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
                    roundId = firstRound.Id,
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
            if (live.LockTime <= now)
            {
                var lockPrice = (await priceService.GetAsync(symbol, "usd", token)).Price;
                live.LockPrice = lockPrice;
                live.LockTime = now;
                live.CloseTime = now.Add(_interval);
                live.Status = RoundStatus.Locked;
                await roundRepo.UpdateByPrimaryKeyAsync(live);
                await priceRepo.InsertAsync(new PriceSnapshotEntity { Symbol = symbol, Timestamp = now, Price = lockPrice });

                var nextPrice = lockPrice;
                var nextRound = new RoundEntity
                {
                    Symbol = symbol,
                    Id = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
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
                    roundId = nextRound.Id,
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
