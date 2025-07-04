using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Services;

namespace TonPrediction.Api.Services
{
    /// <summary>
    /// 定时记录价格快照的后台服务。
    /// </summary>
    public class PriceMonitor(
        IServiceScopeFactory scopeFactory,
        IPriceService priceService,
        ILogger<PriceMonitor> logger) : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private readonly IPriceService _priceService = priceService;
        private readonly ILogger<PriceMonitor> _logger = logger;

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RecordPriceAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Price monitor error");
                }

                await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
            }
        }

        private async Task RecordPriceAsync(CancellationToken token)
        {
            var price = await _priceService.GetCurrentPriceAsync(token);
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IPriceSnapshotRepository>();
            await repo.InsertAsync(new PriceSnapshotEntity
            {
                Timestamp = DateTime.UtcNow,
                Price = price
            });
        }
    }
}
