using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Enums;
using System.Linq;
using SqlSugar;
using System.Collections.Generic;
using TonPrediction.Application.Services.Interface;
using TonPrediction.Application.Cache;
using TonPrediction.Application.Output;

namespace TonPrediction.Api.Services
{
    /// <summary>
    /// 定时记录价格快照的后台服务。
    /// </summary>
    public class PriceMonitor(
        IServiceScopeFactory scopeFactory,
        IPriceService priceService,
        IPredictionHubService notifier,
        ILogger<PriceMonitor> logger,
        IDistributedLock locker, IConfiguration configuration) : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private readonly IPriceService _priceService = priceService;
        private readonly IPredictionHubService _notifier = notifier;
        private readonly ILogger<PriceMonitor> _logger = logger;
        private readonly IDistributedLock _locker = locker;
        private readonly string[] _symbols = configuration.GetSection("Symbols").Get<string[]>()!;

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var handle = await _locker.AcquireAsync(
                        CacheKeyCollection.PriceMonitorLockKey,
                        TimeSpan.FromSeconds(30));
                    if (handle != null)
                    {
                        foreach (var symbol in _symbols)
                        {
                            await RecordPriceAsync(symbol, stoppingToken);
                        }

                        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Price monitor error");
                }


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

            //获取当前锁定的回合
            var round = await roundRepo.GetCurrentLockedAsync(symbol);
            if (round != null)
            {
                await _notifier.PushCurrentRoundAsync(round, price);
            }
        }
    }
}
