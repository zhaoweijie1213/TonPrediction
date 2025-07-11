using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Enums;
using TonPrediction.Application.Output;
using TonPrediction.Application.Services.Interface;
using TonPrediction.Application.Cache;
using System.Text.RegularExpressions;
using TonPrediction.Application.Config;
using TonPrediction.Application.Services;
using TonPrediction.Api.Services.WalletListeners;

namespace TonPrediction.Api.Services;

/// <summary>
/// 监听主钱包入账的后台服务。
/// </summary>
public class TonEventListener(IServiceScopeFactory scopeFactory, IPredictionHubService notifier, ILogger<TonEventListener> logger, IDistributedLock locker,
    IWalletListener walletListener, WalletConfig walletConfig) : BackgroundService
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

    /// <summary>
    /// 钱包地址
    /// </summary>
    private readonly string _walletAddress = walletConfig.ENV_MASTER_WALLET_ADDRESS;

    /// <summary>
    ///
    /// </summary>
    private ulong _lastLt;
    private static readonly Regex CommentRegex = new(@"^\s*(\d+)\s+(bull|bear)\s*$", RegexOptions.IgnoreCase);

    /// <summary>
    /// 执行
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_walletAddress))
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
                using var handle = await _locker.AcquireAsync(
                    CacheKeyCollection.TonEventListenerLockKey,
                    TimeSpan.FromMinutes(5));
                if (handle == null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                    continue;
                }

                await foreach (var tx in _walletListener.ListenAsync(_walletAddress, _lastLt, stoppingToken))
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
        var match = CommentRegex.Match(tx.In_Message.Comment ?? string.Empty);
        if (!match.Success || !long.TryParse(match.Groups[1].Value, out var roundId)) return;
        var side = match.Groups[2].Value.ToLowerInvariant();
        //var roundId = match.Groups[3].Value.ToLowerInvariant();

        var amount = tx.Amount;        // TonAPI 已返回普通 TON
        var sender = tx.In_Message.Source ?? string.Empty;
        var position = side == "bull" ? Position.Bull : Position.Bear;

        using var scope = _scopeFactory.CreateScope();
        var betRepo = scope.ServiceProvider.GetRequiredService<IBetRepository>();
        var roundRepo = scope.ServiceProvider.GetRequiredService<IRoundRepository>();
        var stateRepo = scope.ServiceProvider.GetRequiredService<IStateRepository>();

        var round = await roundRepo.GetByIdAsync(roundId);
        if (round == null || round.Status != RoundStatus.Betting) return;

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
                Reward = 0m,
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
    }
}
