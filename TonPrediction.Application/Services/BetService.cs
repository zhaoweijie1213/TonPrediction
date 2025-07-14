using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using QYQ.Base.Common.ApiResult;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Enums;
using TonPrediction.Application.Services.Interface;
using TonPrediction.Application.Extensions;
using TonPrediction.Application.Common;
using TonSdk.Core;
using TonSdk.Core.Block;
using TonSdk.Core.Boc;
using System.Net.Http.Json;
using TonPrediction.Application.Services.WalletListeners;

namespace TonPrediction.Application.Services;

/// <summary>
/// 处理用户上报下注的业务实现。
/// </summary>
public class BetService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IBetRepository betRepo,
    IRoundRepository roundRepo,
    IPredictionHubService predictionHubService) : IBetService
{
    private readonly HttpClient _http = httpClientFactory.CreateClient("TonApi");
    private readonly string _wallet = configuration["ENV_MASTER_WALLET_ADDRESS"] ?? string.Empty;
    private readonly IBetRepository _betRepo = betRepo;
    private readonly IRoundRepository _roundRepo = roundRepo;

    /// <summary>
    /// Bet 事件备注解析正则。
    /// </summary>
    private static readonly Regex CommentRegex = CommentRegexCollection.Bet;

    /// <summary>
    /// 验证并上报用户下注信息
    /// </summary>
    /// <param name="address">用户钱包地址。</param>
    /// <param name="boc">交易 BOC。</param>
    /// <returns>交易哈希。</returns>
    public async Task<ApiResult<string>> ReportAsync(string address, string boc)
    {
        var result = new ApiResult<string>();
        string msgHash;
        try
        {
            var cell = Cell.From(Base64UrlToBase64(boc));
            msgHash = Convert.ToHexString(cell.Hash.ToBytes()).ToLowerInvariant();
        }
        catch
        {
            result.SetRsult(ApiResultCode.ErrorParams, string.Empty);
            return result;
        }

        TonTxDetail? detail = null;

        //等待交易入块
        var (txHash, lt) = await WaitTxAsync(address, msgHash);

        //获取交易详情
        detail = await FetchDetailAsync(txHash);

        if (detail == null)
        {
            result.SetRsult(ApiResultCode.Fail, string.Empty);
            return result;
        }

        if (!string.Equals(detail.In_Msg?.Destination.Address, _wallet, StringComparison.OrdinalIgnoreCase))
        {
            result.SetRsult(ApiResultCode.ErrorParams, string.Empty);
            return result;
        }
        var text = detail.In_Msg?.Decoded_Body.Text;

        if (string.IsNullOrEmpty(text)) return result.SetRsult(ApiResultCode.Fail, string.Empty, "error comment");

        var match = CommentRegex.Match(text);

        // 解析事件名称、回合 ID 和下注方向
        if (!match.Success || !match.Groups["evt"].Value.Equals("Bet", StringComparison.OrdinalIgnoreCase))
        {
            result.SetRsult(ApiResultCode.ErrorParams, string.Empty);
            return result;
        }
        long roundId = long.Parse(match.Groups["rid"].Value);
        bool isBull = match.Groups["dir"].Value.Equals("bull",
                            StringComparison.OrdinalIgnoreCase);
        var round = await _roundRepo.GetByIdAsync(roundId);
        if (round == null || round.Status != RoundStatus.Betting)
        {
            result.SetRsult(ApiResultCode.Fail, string.Empty);
            return result;
        }

        var position = isBull ? Position.Bull : Position.Bear;
        var bet = new BetEntity
        {
            RoundId = round.Id,
            UserAddress = detail.In_Msg?.Source.Address ?? string.Empty,
            Amount = detail.Amount,
            Position = position,
            Claimed = false,
            Reward = 0,
            TxHash = txHash,
            Lt = detail.Lt,
            Status = BetStatus.Pending
        };
        await _betRepo.InsertAsync(bet);
        result.SetRsult(ApiResultCode.Success, txHash);
        return result;
    }

    /// <summary>
    /// 验证指定回合是否可下注。
    /// </summary>
    /// <param name="roundId">回合编号。</param>
    /// <param name="userAddress"></param>
    /// <returns>验证结果。</returns>
    public async Task<ApiResult<bool>> VerifyAsync(long roundId, string userAddress)
    {
        var api = new ApiResult<bool>();
        var round = await _roundRepo.GetByIdAsync(roundId);
        if (round == null)
        {
            api.SetRsult(ApiResultCode.DataNotFound, false);
            return api;
        }

        var bet = await _betRepo.GetByRoundAndUserAsync(roundId, userAddress);

        if (bet != null)
        {
            api.SetRsult(ApiResultCode.Fail, false, "You have already bet on this round.");
            return api;
        }

        var now = DateTime.UtcNow;
        var ok = round.Status == RoundStatus.Betting && round.LockTime > now;
        api.SetRsult(ok ? ApiResultCode.Success : ApiResultCode.Fail, ok);
        return api;
    }

    /// <summary>
    /// 等待交易入块并返回交易哈希和逻辑时间戳。
    /// </summary>
    /// <param name="address">钱包地址。</param>
    /// <param name="msgHash">消息哈希。</param>
    /// <returns>交易哈希与账户逻辑时间。</returns>
    public async Task<(string txHash, ulong lt)> WaitTxAsync(string address, string msgHash)
    {
        ulong lastLt = 0;
        while (true)
        {
            var url = string.Format(TonApiRoutes.AccountTransactions, address, 20, lastLt);
            var resp = await _http.GetFromJsonAsync<AccountTxList>(url);
            if (resp?.Transactions != null)
            {
                foreach (var tx in resp.Transactions)
                {
                    if (string.Equals(tx.In_Msg?.Hash, msgHash, StringComparison.OrdinalIgnoreCase))
                    {
                        return (tx.Hash, tx.Lt);
                    }
                }
                lastLt = resp.Transactions[0].Lt;
            }
            await Task.Delay(1000); // 每 1s 轮询
        }
    }

    /// <summary>
    /// 拉取指定交易的详细信息。
    /// </summary>
    /// <param name="txHash"></param>
    /// <returns></returns>
    public async Task<TonTxDetail?> FetchDetailAsync(string txHash)
    {
        return await _http.GetFromJsonAsync<TonTxDetail>(string.Format(TonApiRoutes.TransactionDetail, txHash));
    }




    private static string Base64UrlToBase64(string input)
    {
        input = input.Replace('-', '+').Replace('_', '/');
        switch (input.Length % 4)
        {
            case 2: input += "=="; break;
            case 3: input += "="; break;
        }
        return input;
    }
}
