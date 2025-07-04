using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Enums;
using TonPrediction.Application.Services;
using PancakeSwap.Api.Hubs;

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

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await HandleRoundAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Round scheduler error");
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        private async Task HandleRoundAsync(CancellationToken token)
        {
            using var scope = _scopeFactory.CreateScope();
            var roundRepo = scope.ServiceProvider.GetRequiredService<IRoundRepository>();
            var priceRepo = scope.ServiceProvider.GetRequiredService<IPriceSnapshotRepository>();
            var priceService = scope.ServiceProvider.GetRequiredService<IPriceService>();

            var now = DateTime.UtcNow;
            dynamic repoDyn = roundRepo;
            var db = repoDyn.Db;
            var current = await db.Queryable<RoundEntity>()
                .OrderBy("id", SqlSugar.OrderByType.Desc)
                .FirstAsync();

            if (current == null || current.Status == RoundStatus.Ended)
            {
                var startPrice = (await priceService.GetAsync("ton", "usd", token)).Price;
                var newRound = new RoundEntity
                {
                    Id = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    StartTime = now,
                    LockTime = now,
                    CloseTime = now.Add(_interval),
                    LockPrice = startPrice,
                    Status = RoundStatus.Live
                };
                await roundRepo.InsertAsync(newRound);
                await priceRepo.InsertAsync(new PriceSnapshotEntity { Timestamp = now, Price = startPrice });
                await _hub.Clients.All.SendAsync("currentRound", new
                {
                    roundId = newRound.Id,
                    lockPrice = newRound.LockPrice.ToString("F8"),
                    currentPrice = newRound.LockPrice.ToString("F8"),
                    totalAmount = newRound.TotalAmount.ToString("F8"),
                    upAmount = newRound.BullAmount.ToString("F8"),
                    downAmount = newRound.BearAmount.ToString("F8"),
                    rewardPool = newRound.RewardAmount.ToString("F8"),
                    endTime = new DateTimeOffset(newRound.CloseTime).ToUnixTimeSeconds(),
                    oddsUp = "0",
                    oddsDown = "0",
                    status = RoundStatus.Live
                }, token);
                return;
            }

            if (current.Status == RoundStatus.Live && current.CloseTime <= now)
            {
                var closePrice = (await priceService.GetAsync("ton", "usd", token)).Price;
                current.CloseTime = now;
                current.ClosePrice = closePrice;
                current.Status = RoundStatus.Ended;
                await roundRepo.UpdateByPrimaryKeyAsync(current);
                await priceRepo.InsertAsync(new PriceSnapshotEntity { Timestamp = now, Price = closePrice });
                await _hub.Clients.All.SendAsync("roundEnded", new { roundId = current.Id }, token);
            }
        }
    }
}
