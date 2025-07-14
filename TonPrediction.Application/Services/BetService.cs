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
    /// <param name="boc">交易 BOC。</param>
    /// <returns>操作结果。</returns>
    public async Task<ApiResult<bool>> ReportAsync(string boc)
    {
        var api = new ApiResult<bool>();
        string msgHash;
        try
        {
            var cell = Cell.From(Base64UrlToBase64(boc));
            msgHash = Convert.ToHexString(cell.Hash.ToBytes());
        }
        catch
        {
            api.SetRsult(ApiResultCode.ErrorParams, false);
            return api;
        }

        TonTxDetail? detail = null;
        for (var i = 0; i < 5 && detail == null; i++)
        {
            try
            {
                detail = await _http.GetFromJsonAsync<TonTxDetail>(
                    $"/v2/blockchain/messages/{msgHash}/transaction");
            }
            catch
            {
                detail = null;
            }

            if (detail == null)
                await Task.Delay(1000);
        }

        if (detail == null)
        {
            api.SetRsult(ApiResultCode.Fail, false);
            return api;
        }

        if (!string.Equals(detail.In_Msg?.Destination.Address, _wallet, StringComparison.OrdinalIgnoreCase))
        {
            api.SetRsult(ApiResultCode.ErrorParams, false);
            return api;
        }
        var text = detail.In_Msg?.Decoded_Body.Text;

        var match = CommentRegex.Match(text);

        // 解析事件名称、回合 ID 和下注方向
        if (!match.Success || !match.Groups["evt"].Value.Equals("Bet", StringComparison.OrdinalIgnoreCase))
        {
            api.SetRsult(ApiResultCode.ErrorParams, false);
            return api;
        }
        long roundId = long.Parse(match.Groups["rid"].Value);
        bool isBull = match.Groups["dir"].Value.Equals("bull",
                            StringComparison.OrdinalIgnoreCase);
        var round = await _roundRepo.GetByIdAsync(roundId);
        if (round == null || round.Status != RoundStatus.Betting)
        {
            api.SetRsult(ApiResultCode.Fail, false);
            return api;
        }
        var txHash = detail.Hash;
        if (await _betRepo.GetByTxHashAsync(txHash) != null)
        {
            api.SetRsult(ApiResultCode.ErrorParams, false, "TxHash is exist");
            return api;
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
        api.SetRsult(ApiResultCode.Success, true);
        return api;
    }

    /// <summary>
    /// 验证指定回合是否可下注。
    /// </summary>
    /// <param name="roundId">回合编号。</param>
    /// <returns>验证结果。</returns>
    public async Task<ApiResult<bool>> VerifyAsync(long roundId)
    {
        var api = new ApiResult<bool>();
        var round = await _roundRepo.GetByIdAsync(roundId);
        if (round == null)
        {
            api.SetRsult(ApiResultCode.DataNotFound, false);
            return api;
        }

        var now = DateTime.UtcNow;
        var ok = round.Status == RoundStatus.Betting && round.LockTime > now;
        api.SetRsult(ok ? ApiResultCode.Success : ApiResultCode.Fail, ok);
        return api;
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
