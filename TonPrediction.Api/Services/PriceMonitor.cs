using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Enums;
using PancakeSwap.Api.Hubs;
using System.Linq;
using SqlSugar;
using System.Collections.Generic;
using TonPrediction.Application.Services.Interface;

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
                        await RecordPriceAsync(symbol, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Price monitor error");
                }

                await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
            }
        }

        /// <summary>
        /// 价格快照记录方法。
        /// </summary>
        /// <param name="symbol">币种符号。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>任务。</returns>
        private async Task RecordPriceAsync(string symbol, CancellationToken token)
        {
            var priceResult = await _priceService.GetAsync(symbol, "usd", token);
            var price = priceResult.Price;
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IPriceSnapshotRepository>();
            var roundRepo = scope.ServiceProvider.GetRequiredService<IRoundRepository>();
            await repo.InsertAsync(new PriceSnapshotEntity
            {
                Symbol = symbol,
                Timestamp = DateTime.UtcNow,
                Price = price
            });

            var since = DateTime.UtcNow.AddMinutes(-10);
            var data = await repo.GetSinceAsync(symbol, since, token);
            var timestamps = data.Select(d => new DateTimeOffset(d.Timestamp).ToUnixTimeSeconds()).ToArray();
            var prices = data.Select(d => d.Price.ToString("F8")).ToArray();
            await _hub.Clients.All.SendAsync("chartData", new { timestamps, prices }, token);

            var round = await roundRepo.GetCurrentLiveAsync(symbol, token);
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
