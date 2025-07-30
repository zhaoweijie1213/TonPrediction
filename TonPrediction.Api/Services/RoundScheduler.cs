using DotNetCore.CAP;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using TonPrediction.Application.Cache;
using TonPrediction.Application.Config;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Enums;
using TonPrediction.Application.Events;
using TonPrediction.Application.Services.Interface;

namespace TonPrediction.Api.Services
{
    /// <summary>
    /// 定期创建和结束回合的后台服务，具备容错恢复能力。
    /// </summary>
    public class RoundScheduler(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        IPredictionHubService notifier,
        ILogger<RoundScheduler> logger,
        IDistributedLock locker,
        ICapPublisher publisher, IOptionsMonitor<PredictionConfig> predictionConfig) : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private readonly IPredictionHubService _notifier = notifier;
        private readonly ILogger<RoundScheduler> _logger = logger;
        private readonly IDistributedLock _locker = locker;
        private readonly ICapPublisher _publisher = publisher;

        /// <summary>
        /// 手续费率，默认为 3%。
        /// </summary>
        private readonly decimal _treasuryFeeRate = configuration.GetValue<decimal>("TreasuryFeeRate", 0.03m);

        /// <summary>
        /// 时间间隔，单位为秒。根据配置文件中的 RoundIntervalSeconds 获取。
        /// </summary>
        private readonly int _interval = predictionConfig.CurrentValue.RoundIntervalSeconds;

        /// <summary>
        /// 执行的币种列表。根据配置文件中的 Symbols 获取。
        /// </summary>
        private readonly string[] _symbols = configuration.GetSection("Symbols").Get<string[]>()!;

        /// <summary>
        /// 执行任务的入口点。此方法会在服务启动时被调用
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var tasks = _symbols.Select(s => Task.Run(() => RunSymbolLoopAsync(s, stoppingToken), stoppingToken)).ToArray();
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// 执行每个币种的回合循环
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task RunSymbolLoopAsync(string symbol, CancellationToken token)
        {
            // 确保创世回合已执行
            await ExcuteGenesisRoundAsync(symbol);

            //正常执行回合
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using var handle = await _locker.AcquireAsync(
                        CacheKeyCollection.GetRoundSchedulerLockKey(symbol),
                        TimeSpan.FromSeconds(10));
                    if (handle != null)
                    {
                        await ExecuteRoundAsync(symbol, token);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(1), token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Round scheduler error for {Symbol}", symbol);
                }


            }
        }

        /// <summary>
        /// 执行创世回合
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public async Task ExcuteGenesisRoundAsync(string symbol)
        {
            using var scope = _scopeFactory.CreateScope();
            var roundRepo = scope.ServiceProvider.GetRequiredService<IRoundRepository>();

            // 创世回合：给予首轮下注窗口
            var genesisRound = await roundRepo.GetGenesisRoundAsync(symbol);
            RoundEntity round;
            if (genesisRound == null)
            {
                var now = DateTime.UtcNow;
                //var priceService = scope.ServiceProvider.GetRequiredService<IPriceService>();
                //var startPrice = (await priceService.GetAsync(symbol, "usd")).Price;

                round = new RoundEntity
                {
                    Symbol = symbol,
                    Epoch = 1,
                    StartTime = now,
                    LockTime = now.AddSeconds(_interval),
                    CloseTime = now.AddSeconds(_interval * 2),
                    //LockPrice = startPrice,
                    Status = RoundStatus.Betting
                };
                await roundRepo.InsertAsync(round);
            }
        }


        /// <summary>
        /// 回合处理
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task ExecuteRoundAsync(string symbol, CancellationToken token)
        {
            using var scope = _scopeFactory.CreateScope();
            var roundRepo = scope.ServiceProvider.GetRequiredService<IRoundRepository>();
            var priceService = scope.ServiceProvider.GetRequiredService<IPriceService>();
            var betRepo = scope.ServiceProvider.GetRequiredService<IBetRepository>();

            var now = DateTime.UtcNow;

            // 结束上一回合并结算奖励
            var locked = await roundRepo.GetCurrentLockedAsync(symbol);
            if (locked?.Status == RoundStatus.Locked && locked.CloseTime <= now)
            {
                await _notifier.PushSettlementStartedAsync(locked.Id, locked.Epoch);

                var closePrice = (await priceService.GetAsync(symbol, "usd", token)).Price;
                locked.CloseTime = now;
                locked.ClosePrice = closePrice;
                locked.Status = RoundStatus.Calculating;

                //计算获胜方
                Position? winner = null;
                if (closePrice > locked.LockPrice) winner = Position.Bull;
                else if (closePrice < locked.LockPrice) winner = Position.Bear;
                else if (closePrice == locked.LockPrice) winner = Position.Tie;

                locked.WinnerSide = winner;
                locked.RewardBaseCalAmount = winner switch
                {
                    Position.Bull => locked.BullAmount,
                    Position.Bear => locked.BearAmount,
                    _ => locked.TotalAmount
                };
                locked.RewardAmount = locked.TotalAmount;
                await roundRepo.UpdateByPrimaryKeyAsync(locked);


                //计算奖励并更新下注记录
                var bets = await betRepo.GetByRoundAsync(locked.Id, token);
                var winTotal = winner switch
                {
                    Position.Bull => locked.BullAmount,
                    Position.Bear => locked.BearAmount,
                    _ => 0L
                };

                foreach (var bet in bets)
                {
                    long reward = 0;
                    if (!winner.HasValue)
                    {
                        reward = bet.Amount;
                    }
                    else if ((winner == Position.Bull && bet.Position == Position.Bull) || (winner == Position.Bear && bet.Position == Position.Bear))
                    {
                        var totalReward = winTotal > 0 ? (bet.Amount * (decimal)(locked.RewardAmount / winTotal)) : 0;
                        // 计算奖励金额，扣除手续费
                        reward = (long)(totalReward * (1m - _treasuryFeeRate));

                        bet.TreasuryFee = (long)(totalReward - reward);
                    }
                    bet.Reward = reward;

                    // 平局时仅计算返还金额，具体转账在异步事件中处理

                    await betRepo.UpdateByPrimaryKeyAsync(bet);
                }

                await _publisher.PublishAsync("round.stat.update", new RoundStatEvent(symbol, locked.Id));

                if (winner == Position.Tie)
                {
                    await _publisher.PublishAsync("round.refund.tie", new RoundRefundEvent(symbol, locked.Id));
                }

                locked.Status = winner switch
                {
                    Position.Tie => RoundStatus.Cancelled,
                    _ => RoundStatus.Completed
                };

                await roundRepo.UpdateByPrimaryKeyAsync(locked);

                await _notifier.PushSettlementEndedAsync(locked.Id, locked.Epoch);

                //await _notifier.PushRoundEndedAsync(locked.Id, locked.Epoch);
            }

            // 获取当前正在下注的回合
            var live = await roundRepo.GetCurrentBettingAsync(symbol);
            // 到达锁定时间时，锁定当前回合并创建下一回合
            if (live?.LockTime <= now && live.Status == RoundStatus.Betting)
            {
                decimal lockPrice;
                // 锁定当前回合
                if (locked?.LockPrice > 0)
                {
                    lockPrice = locked.ClosePrice;
                }
                else
                {
                    lockPrice = (await priceService.GetAsync(symbol, "usd", token)).Price;
                }
                live.LockPrice = lockPrice;
                live.Status = RoundStatus.Locked;
                await roundRepo.UpdateByPrimaryKeyAsync(live);
                await _notifier.PushRoundLockedAsync(live.Id, live.Epoch);

                // 创建下一回合
                var nextPrice = lockPrice;
                long nextEpoch = live.Epoch + 1;
                var nextRound = await roundRepo.GetByEpochAsync(symbol, nextEpoch);
                if (nextRound != null)
                {
                    nextRound.StartTime = now;
                    nextRound.LockTime = now.AddSeconds(_interval);
                    nextRound.CloseTime = now.AddSeconds(_interval * 2);
                    nextRound.LockPrice = nextPrice;
                    nextRound.Status = RoundStatus.Betting;
                    await roundRepo.UpdateByPrimaryKeyAsync(nextRound);
                }
                else
                {
                    nextRound = new RoundEntity
                    {
                        Symbol = symbol,
                        Epoch = nextEpoch,
                        StartTime = now,
                        LockTime = now.AddSeconds(_interval),
                        CloseTime = now.AddSeconds(_interval * 2),
                        LockPrice = nextPrice,
                        Status = RoundStatus.Betting
                    };
                    await roundRepo.InsertAsync(nextRound);
                }

            }
        }
    }
}
