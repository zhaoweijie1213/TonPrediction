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
            var current = await roundRepo.GetLatestAsync(symbol, token);

            if (current == null || current.Status == RoundStatus.Ended)
            {
                var startPrice = (await priceService.GetAsync(symbol, "usd", token)).Price;
                var newRound = new RoundEntity
                {
                    Symbol = symbol,
                    Id = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    StartTime = now,
                    LockTime = now,
                    CloseTime = now.Add(_interval),
                    LockPrice = startPrice,
                    Status = RoundStatus.Live
                };
                await roundRepo.InsertAsync(newRound);
                await priceRepo.InsertAsync(new PriceSnapshotEntity { Symbol = symbol, Timestamp = now, Price = startPrice });
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
                var closePrice = (await priceService.GetAsync(symbol, "usd", token)).Price;
                current.CloseTime = now;
                current.ClosePrice = closePrice;
                current.Status = RoundStatus.Ended;
                await roundRepo.UpdateByPrimaryKeyAsync(current);
                await priceRepo.InsertAsync(new PriceSnapshotEntity { Symbol = symbol, Timestamp = now, Price = closePrice });
                await _hub.Clients.All.SendAsync("roundEnded", new { roundId = current.Id }, token);
            }
        }
    }
}
