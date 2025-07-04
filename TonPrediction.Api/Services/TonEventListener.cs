using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TonSdk.Client;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Enums;
using PancakeSwap.Api.Hubs;
using SqlSugar;

namespace TonPrediction.Api.Services
{
    /// <summary>
    /// 监听主钱包入账的后台服务。
    /// </summary>
    public class TonEventListener : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<PredictionHub> _hub;
        private readonly ILogger<TonEventListener> _logger;
        private readonly string _walletAddress;
        private readonly dynamic? _client;

        /// <summary>
        /// 初始化监听器。
        /// </summary>
        public TonEventListener(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            IHubContext<PredictionHub> hub,
            ILogger<TonEventListener> logger)
        {
            _scopeFactory = scopeFactory;
            _hub = hub;
            _logger = logger;
            _walletAddress = configuration["ENV_MASTER_WALLET_ADDRESS"] ?? string.Empty;
            var type = Type.GetType("TonSdk.Client.LiteClient, TonSdk.Client");
            _client = type != null ? Activator.CreateInstance(type, configuration["ENV_TON_NODE"] ?? string.Empty) : null;
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_client is null)
                return;

            var enumerable = (IAsyncEnumerable<dynamic>)_client.SubscribeTransactions(_walletAddress, stoppingToken);
            await foreach (var tx in enumerable)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                try
                {
                    var comment = ((string?)tx.Message?.Comment)?.Trim().ToLowerInvariant();
                    if (comment is not ("bull" or "bear"))
                        continue;

                    var amount = (decimal)tx.Amount;
                    var sender = (string?)tx.Message?.Source ?? string.Empty;
                    var position = comment == "bull" ? Position.Bull : Position.Bear;

                    using var scope = _scopeFactory.CreateScope();
                    var betRepo = scope.ServiceProvider.GetRequiredService<IBetRepository>();
                    var roundRepo = scope.ServiceProvider.GetRequiredService<IRoundRepository>();

                    dynamic repoDyn = roundRepo;
                    var db = repoDyn.Db;
                    var round = await db.Queryable<RoundEntity>()
                        .Where("status = @status", new { status = (int)RoundStatus.Live })
                        .OrderBy("id", SqlSugar.OrderByType.Desc)
                        .FirstAsync();
                    if (round == null)
                        continue;

                    await betRepo.InsertAsync(new BetEntity
                    {
                        Epoch = round.Id,
                        UserAddress = sender,
                        Amount = amount,
                        Position = position,
                        Claimed = false,
                        Reward = 0m
                    });

                    round.TotalAmount += amount;
                    if (position == Position.Bull)
                        round.BullAmount += amount;
                    else
                        round.BearAmount += amount;

                    round.RewardAmount = round.TotalAmount;
                    await roundRepo.UpdateByPrimaryKeyAsync(round);

                    var oddsBull = round.BullAmount > 0m ? round.TotalAmount / round.BullAmount : 0m;
                    var oddsBear = round.BearAmount > 0m ? round.TotalAmount / round.BearAmount : 0m;

                    await _hub.Clients.All.SendAsync(
                        "currentRound",
                        new
                        {
                            roundId = round.Id,
                            lockPrice = round.LockPrice.ToString("F8"),
                            currentPrice = round.ClosePrice > 0m ? round.ClosePrice.ToString("F8") : round.LockPrice.ToString("F8"),
                            totalAmount = round.TotalAmount.ToString("F8"),
                            upAmount = round.BullAmount.ToString("F8"),
                            downAmount = round.BearAmount.ToString("F8"),
                            rewardPool = round.RewardAmount.ToString("F8"),
                            endTime = new DateTimeOffset(round.CloseTime).ToUnixTimeSeconds(),
                            oddsUp = oddsBull.ToString("F8"),
                            oddsDown = oddsBear.ToString("F8"),
                            status = round.Status
                        },
                        stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Process transaction failed");
                }
            }
        }
    }
}
