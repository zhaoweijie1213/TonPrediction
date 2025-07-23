using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using TonPrediction.Application.Cache;
using TonPrediction.Application.Common;
using TonPrediction.Application.Config;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Enums;
using TonPrediction.Application.Extensions;
using TonPrediction.Application.Services;
using TonPrediction.Application.Services.Interface;

namespace TonPrediction.Api.Services;

/// <summary>
/// 监听主钱包入账的后台服务。
/// </summary>
public class TonEventListener(IServiceScopeFactory scopeFactory, IPredictionHubService notifier, ILogger<TonEventListener> logger, IDistributedLock locker,
    IWalletListener walletListener, IOptions<WalletConfig> walletConfig, IOptionsMonitor<PredictionConfig> predictionConfig) : BackgroundService
{

    /// <summary>
    /// 
    /// </summary>
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    /// <summary>
    /// 
    /// </summary>
    private readonly IPredictionHubService _notifier = notifier;

    /// <summary>
    /// 
    /// </summary>
    private readonly ILogger<TonEventListener> _logger = logger;

    /// <summary>
    /// 钱包监听实现。
    /// </summary>
    private readonly IWalletListener _walletListener = walletListener;

    /// <summary>
    /// 
    /// </summary>
    private readonly IDistributedLock _locker = locker;

    private readonly IOptionsMonitor<PredictionConfig> _predictionConfig = predictionConfig;

    /// <summary>
    ///
    /// </summary>
    private ulong _lastLt;

    /// <summary>
    /// Bet 事件备注解析正则
    /// </summary>
    private static readonly Regex CommentRegex = CommentRegexCollection.Bet;

    /// <summary>
    /// 执行
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(walletConfig.Value.MasterWalletAddress))
        {
            _logger.LogWarning("主钱包地址未配置，监听器退出");
            return;
        }

        using (var scope = _scopeFactory.CreateScope())
        {
            var stateRepo = scope.ServiceProvider.GetRequiredService<IStateRepository>();
            var val = await stateRepo.GetValueAsync(CacheKeyCollection.TonEventListenerLastLtKey);
            if (ulong.TryParse(val, out var saved))
            {
                _lastLt = saved;
            }
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                //using var handle = await _locker.AcquireAsync(
                //    CacheKeyCollection.TonEventListenerLockKey,
                //    TimeSpan.FromMinutes(5));
                //if (handle == null)
                //{
                //    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                //    continue;
                //}

                await foreach (var tx in _walletListener.ListenAsync(walletConfig.Value.MasterWalletAddress, _lastLt, stoppingToken))
                {
                    await ProcessTransactionAsync(tx);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "钱包监听错误");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }


    /// <summary>
    /// 处理一笔入账交易。
    /// </summary>
    /// <param name="tx">交易详情。</param>
    internal virtual async Task ProcessTransactionAsync(TonTxDetail tx)
    {
        var text = tx.In_Msg?.Decoded_Body.Text;
        if (string.IsNullOrEmpty(text)) return;
        var match = CommentRegex.Match(text);
        if (!match.Success || !match.Groups["evt"].Value.Equals("Bet", StringComparison.OrdinalIgnoreCase)) return;

        // 解析事件名称、回合 ID 和下注方向
        //string eventName = match.Groups["evt"].Value;
        long roundId = long.Parse(match.Groups["rid"].Value);
        bool isBull = match.Groups["dir"].Value.Equals("bull",
                            StringComparison.OrdinalIgnoreCase);

        //var side = match.Groups[2].Value.ToLowerInvariant();
        //var roundId = match.Groups[3].Value.ToLowerInvariant();

        var amount = tx.Amount;        // 转换为 nano TON 存储
        var sender = tx.In_Msg?.Source.Address.ToRawAddress() ?? string.Empty;
        var position = isBull ? Position.Bull : Position.Bear;

        using var scope = _scopeFactory.CreateScope();
        var betRepo = scope.ServiceProvider.GetRequiredService<IBetRepository>();
        var roundRepo = scope.ServiceProvider.GetRequiredService<IRoundRepository>();
        var stateRepo = scope.ServiceProvider.GetRequiredService<IStateRepository>();

        var round = await roundRepo.GetByIdAsync(roundId);
        if (round == null) return;

        var txTime = DateTimeOffset.FromUnixTimeSeconds((long)tx.Utime).UtcDateTime;
        if (txTime > round.LockTime.AddSeconds(_predictionConfig.CurrentValue.BetTimeToleranceSeconds)) return;

        var exist = await betRepo.GetByTxHashAsync(tx.Hash);
        if (exist != null)
        {
            exist.Status = BetStatus.Confirmed;
            exist.Lt = tx.Lt;
            await betRepo.UpdateByPrimaryKeyAsync(exist);
            _lastLt = tx.Lt;
            await stateRepo.SetValueAsync(CacheKeyCollection.TonEventListenerLastLtKey, _lastLt.ToString());
        }
        else
        {
            await betRepo.InsertAsync(new BetEntity
            {
                RoundId = round.Id,
                UserAddress = sender,
                Amount = amount,
                Position = position,
                Claimed = false,
                Reward = 0,
                TxHash = tx.Hash,
                Lt = tx.Lt,
                Status = BetStatus.Confirmed
            });
            _lastLt = tx.Lt;
            await stateRepo.SetValueAsync(CacheKeyCollection.TonEventListenerLastLtKey, _lastLt.ToString());
        }

        round.TotalAmount += amount;
        if (position == Position.Bull) round.BullAmount += amount;
        else round.BearAmount += amount;

        round.RewardAmount = round.TotalAmount;
        await roundRepo.UpdateByPrimaryKeyAsync(round);

        var currentPrice = round.ClosePrice > 0 ? round.ClosePrice : round.LockPrice;
        await _notifier.PushNextRoundAsync(round, currentPrice);
        await _notifier.PushBetPlacedAsync(sender, round.Id, round.Epoch, amount, tx.Hash);
    }
}
