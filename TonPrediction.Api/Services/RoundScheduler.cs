using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Enums;
using TonPrediction.Application.Services;

namespace TonPrediction.Api.Services
{
    /// <summary>
    /// 定期创建和结束回合的后台服务。
    /// </summary>
    public class RoundScheduler(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<RoundScheduler> logger) : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
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
                    await RunRoundAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Round scheduler error");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }

        private async Task RunRoundAsync(CancellationToken token)
        {
            using var scope = _scopeFactory.CreateScope();
            var roundRepo = scope.ServiceProvider.GetRequiredService<IRoundRepository>();
            var priceRepo = scope.ServiceProvider.GetRequiredService<IPriceSnapshotRepository>();
            var priceService = scope.ServiceProvider.GetRequiredService<IPriceService>();

            var startPrice = await priceService.GetCurrentPriceAsync(token);
            var now = DateTime.UtcNow;
            var round = new RoundEntity
            {
                Id = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                StartTime = now,
                LockTime = now,
                CloseTime = now.Add(_interval),
                LockPrice = startPrice,
                Status = RoundStatus.Live
            };
            await roundRepo.InsertAsync(round);
            await priceRepo.InsertAsync(new PriceSnapshotEntity { Timestamp = now, Price = startPrice });

            await Task.Delay(_interval, token);

            var closePrice = await priceService.GetCurrentPriceAsync(token);
            var closeTime = DateTime.UtcNow;
            round.CloseTime = closeTime;
            round.ClosePrice = closePrice;
            round.Status = RoundStatus.Ended;
            await roundRepo.UpdateByPrimaryKeyAsync(round);
            await priceRepo.InsertAsync(new PriceSnapshotEntity { Timestamp = closeTime, Price = closePrice });
        }
    }
}
