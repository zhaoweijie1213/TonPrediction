using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Services;
using TonPrediction.Application.Enums;
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
            var roundRepo = scope.ServiceProvider.GetRequiredService<IRoundRepository>();
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

            dynamic roundDyn = roundRepo;
            var rdb = roundDyn.Db;
            var round = await rdb.Queryable<RoundEntity>()
                .Where("status = @status", new { status = (int)RoundStatus.Live })
                .OrderBy("id", OrderByType.Desc)
                .FirstAsync();
            if (round != null)
            {
                var oddsBull = round.BullAmount > 0m ? round.TotalAmount / round.BullAmount : 0m;
                var oddsBear = round.BearAmount > 0m ? round.TotalAmount / round.BearAmount : 0m;
                await _hub.Clients.All.SendAsync("currentRound", new
                {
                    roundId = round.Id,
                    lockPrice = round.LockPrice.ToString("F8"),
                    currentPrice = price.ToString("F8"),
                    totalAmount = round.TotalAmount.ToString("F8"),
                    upAmount = round.BullAmount.ToString("F8"),
                    downAmount = round.BearAmount.ToString("F8"),
                    rewardPool = round.RewardAmount.ToString("F8"),
                    endTime = new DateTimeOffset(round.CloseTime).ToUnixTimeSeconds(),
                    oddsUp = oddsBull.ToString("F8"),
                    oddsDown = oddsBear.ToString("F8"),
                    status = round.Status
                }, token);
            }
        }
    }
}
