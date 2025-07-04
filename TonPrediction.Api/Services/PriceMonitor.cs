using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Services;
using PancakeSwap.Api.Hubs;
using System.Linq;
using SqlSugar;
using System.Collections.Generic;

namespace TonPrediction.Api.Services
{
    /// <summary>
    /// 定时记录价格快照的后台服务。
    /// </summary>
    public class PriceMonitor(
        IServiceScopeFactory scopeFactory,
        IPriceService priceService,
        IHubContext<PredictionHub> hub,
        ILogger<PriceMonitor> logger) : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private readonly IPriceService _priceService = priceService;
        private readonly IHubContext<PredictionHub> _hub = hub;
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
            var priceResult = await _priceService.GetAsync("ton", "usd", token);
            var price = priceResult.Price;
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IPriceSnapshotRepository>();
            await repo.InsertAsync(new PriceSnapshotEntity
            {
                Timestamp = DateTime.UtcNow,
                Price = price
            });

            var since = DateTime.UtcNow.AddMinutes(-10);
            dynamic repoDyn = repo;
            var db = repoDyn.Db;
            var data = (List<PriceSnapshotEntity>)await db.Queryable<PriceSnapshotEntity>()
                .Where("timestamp >= @ts", new { ts = since })
                .OrderBy("timestamp", OrderByType.Asc)
                .ToListAsync();
            var timestamps = data.Select(d => new DateTimeOffset(d.Timestamp).ToUnixTimeSeconds()).ToArray();
            var prices = data.Select(d => d.Price.ToString("F8")).ToArray();
            await _hub.Clients.All.SendAsync("chartData", new { timestamps, prices }, token);
        }
    }
}
