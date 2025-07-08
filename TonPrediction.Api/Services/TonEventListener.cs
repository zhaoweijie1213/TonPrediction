using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using PancakeSwap.Api.Hubs;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Enums;
using TonPrediction.Application.Output;
using TonPrediction.Application.Services.Interface;
using TonPrediction.Application.Cache;
using System.Text.RegularExpressions;

namespace TonPrediction.Api.Services;

/// <summary>
/// 监听主钱包入账的后台服务。
/// </summary>
public class TonEventListener(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    IHubContext<PredictionHub> hub,
    ILogger<TonEventListener> logger,
    IHttpClientFactory httpFactory,
    IDistributedLock locker) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IHubContext<PredictionHub> _hub = hub;
    private readonly ILogger<TonEventListener> _logger = logger;
    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly IDistributedLock _locker = locker;
    private readonly string _walletAddress = configuration["ENV_MASTER_WALLET_ADDRESS"] ?? string.Empty;
    private ulong _lastLt;
    private const string SseUrlTemplate =
        "/v2/sse/accounts/transactions?accounts={0}";
    private static readonly Regex CommentRegex = new(@"^\s*(\w+)\s+(bull|bear)\s*$", RegexOptions.IgnoreCase);

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

        var backoff = TimeSpan.FromSeconds(3);
        var http = _httpFactory.CreateClient("TonApi");

        using (var scope = _scopeFactory.CreateScope())
        {
            var stateRepo = scope.ServiceProvider.GetRequiredService<IStateRepository>();
            var val = await stateRepo.GetValueAsync(CacheKeyCollection.TonEventListenerLastLtKey, stoppingToken);
            if (ulong.TryParse(val, out var saved))
            {
                _lastLt = saved;
                await FetchMissedAsync(http, stoppingToken);
            }
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var handle = await _locker.AcquireAsync(
                    CacheKeyCollection.TonEventListenerLockKey,
                    TimeSpan.FromMinutes(5),
                    stoppingToken);
                if (handle == null)
                {
                    await Task.Delay(backoff, stoppingToken);
                    continue;
                }

                await using var stream = await http.GetStreamAsync(
                    string.Format(SseUrlTemplate, _walletAddress), stoppingToken);
                using var reader = new StreamReader(stream);

                string? eventName = null;
                while (!reader.EndOfStream && !stoppingToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(line)) continue;

                    if (line.StartsWith("event:"))
                    {
                        eventName = line["event:".Length..].Trim();
                        continue;
                    }

                    if (line.StartsWith("data:") && eventName == "message")
                    {
                        var json = line["data:".Length..].Trim();
                        var head = JsonConvert.DeserializeObject<SseTxHead>(json)!;

                        // 拉取完整交易详情
                        var detail = await http
                            .GetFromJsonAsync<TonTxDetail>(
                                $"/v2/blockchain/transactions/{head.Tx_Hash}",
                                stoppingToken);

                        if (detail != null)
                        {
                            detail = detail with { Hash = head.Tx_Hash, Lt = head.Lt };
                            await ProcessTransactionAsync(detail, stoppingToken);
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SSE 连接中断，{Delay}s 后重试…", backoff.TotalSeconds);
                await FetchMissedAsync(http, stoppingToken);
                await Task.Delay(backoff, stoppingToken);
                backoff = TimeSpan.FromSeconds(Math.Min(backoff.TotalSeconds * 2, 30));
            }
        }
    }

    /// <summary>
    /// 拉取未处理的历史交易
    /// </summary>
    /// <param name="http"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    private async Task FetchMissedAsync(HttpClient http, CancellationToken ct)
    {
        if (_lastLt == 0) return;
        var url = $"/v2/blockchain/accounts/{_walletAddress}/transactions?limit=20&to_lt={_lastLt}";
        try
        {
            var resp = await http.GetFromJsonAsync<AccountTxList>(url, ct);
            if (resp?.Transactions != null)
            {
                foreach (var tx in resp.Transactions)
                {
                    await ProcessTransactionAsync(tx with { Lt = tx.Lt }, ct);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "拉取历史交易失败");
        }
    }

    /// <summary>
    /// 处理一笔入账交易。
    /// </summary>
    /// <param name="tx">交易详情。</param>
    /// <param name="ct">取消令牌。</param>
    internal virtual async Task ProcessTransactionAsync(TonTxDetail tx, CancellationToken ct)
    {
        var match = CommentRegex.Match(tx.In_Message.Comment ?? string.Empty);
        if (!match.Success) return;
        var symbol = match.Groups[1].Value.ToLowerInvariant();
        var side = match.Groups[2].Value.ToLowerInvariant();

        var amount = tx.Amount;        // TonAPI 已返回普通 TON
        var sender = tx.In_Message.Source ?? string.Empty;
        var position = side == "bull" ? Position.Bull : Position.Bear;

        using var scope = _scopeFactory.CreateScope();
        var betRepo = scope.ServiceProvider.GetRequiredService<IBetRepository>();
        var roundRepo = scope.ServiceProvider.GetRequiredService<IRoundRepository>();
        var stateRepo = scope.ServiceProvider.GetRequiredService<IStateRepository>();

        var round = await roundRepo.GetCurrentLiveAsync(symbol, ct);
        if (round == null) return;

        var exist = await betRepo.GetByTxHashAsync(tx.Hash, ct);
        if (exist != null)
        {
            exist.Status = BetStatus.Confirmed;
            exist.Lt = tx.Lt;
            await betRepo.UpdateByPrimaryKeyAsync(exist);
            _lastLt = tx.Lt;
            await stateRepo.SetValueAsync(CacheKeyCollection.TonEventListenerLastLtKey, _lastLt.ToString(), ct);
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
            await stateRepo.SetValueAsync(CacheKeyCollection.TonEventListenerLastLtKey, _lastLt.ToString(), ct);
        }

        round.TotalAmount += amount;
        if (position == Position.Bull) round.BullAmount += amount;
        else round.BearAmount += amount;

        round.RewardAmount = round.TotalAmount;
        await roundRepo.UpdateByPrimaryKeyAsync(round);

        var oddsBull = round.BullAmount > 0 ? round.TotalAmount / round.BullAmount : 0;
        var oddsBear = round.BearAmount > 0 ? round.TotalAmount / round.BearAmount : 0;

        await _hub.Clients.All.SendAsync(
            "currentRound",
            new CurrentRoundOutput
            {
                RoundId = round.Epoch,
                LockPrice = round.LockPrice.ToString("F8"),
                CurrentPrice = round.ClosePrice > 0 ? round.ClosePrice.ToString("F8") : round.LockPrice.ToString("F8"),
                TotalAmount = round.TotalAmount.ToString("F8"),
                BullAmount = round.BullAmount.ToString("F8"),
                BearAmount = round.BearAmount.ToString("F8"),
                RewardPool = round.RewardAmount.ToString("F8"),
                EndTime = new DateTimeOffset(round.CloseTime).ToUnixTimeSeconds(),
                BullOdds = oddsBull.ToString("F8"),
                BearOdds = oddsBear.ToString("F8"),
                Status = round.Status
            }, ct);
    }



}

/// <summary>
/// TonAPI SSE “message” 事件载荷
/// </summary>
/// <param name="Account_Id"></param>
/// <param name="Lt"></param>
/// <param name="Tx_Hash"></param>
public record SseTxHead(string Account_Id, ulong Lt, string Tx_Hash);

/// <summary>
/// TonAPI /v2/blockchain/transactions/{hash} 响应 (只列用到的字段)
/// </summary>
/// <param name="Amount">交易金额（nanoTON 已转普通 TON）</param>
/// <param name="In_Message"></param>
/// <param name="Hash">交易哈希。</param>
public record TonTxDetail(
    decimal Amount,
    InMsg In_Message,
    string Hash)
{
    /// <summary>
    /// 交易的账户逻辑时间。
    /// </summary>
    public ulong Lt { get; init; }
}

/// <summary>
/// 
/// </summary>
/// <param name="Source"></param>
/// <param name="Comment"></param>
/// <param name="Destination"></param>
public record InMsg(string? Source, string? Comment, string? Destination);

/// <summary>
/// TonAPI 账户交易列表响应。
/// </summary>
/// <param name="Transactions">交易数组。</param>
public record AccountTxList(TonTxDetail[] Transactions);
