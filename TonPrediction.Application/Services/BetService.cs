using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Enums;
using TonPrediction.Application.Services.Interface;

namespace TonPrediction.Application.Services;

/// <summary>
/// 处理用户上报下注的业务实现。
/// </summary>
public class BetService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IBetRepository betRepo,
    IRoundRepository roundRepo) : IBetService
{
    private readonly HttpClient _http = httpClientFactory.CreateClient("TonApi");
    private readonly string _wallet = configuration["ENV_MASTER_WALLET_ADDRESS"] ?? string.Empty;
    private readonly IBetRepository _betRepo = betRepo;
    private readonly IRoundRepository _roundRepo = roundRepo;
    private static readonly Regex CommentRegex = new(@"^\s*(\w+)\s+(bull|bear)\s*$", RegexOptions.IgnoreCase);

    /// <inheritdoc />
    public async Task<bool> ReportAsync(string txHash, CancellationToken ct = default)
    {
        var detail = await _http.GetFromJsonAsync<TonTxDetail>($"/v2/blockchain/transactions/{txHash}", ct);
        if (detail == null) return false;
        if (!string.Equals(detail.In_Message?.Destination, _wallet, StringComparison.OrdinalIgnoreCase))
            return false;
        var match = CommentRegex.Match(detail.In_Message?.Comment ?? string.Empty);
        if (!match.Success) return false;
        var symbol = match.Groups[1].Value.ToLowerInvariant();
        var side = match.Groups[2].Value.ToLowerInvariant();
        var round = await _roundRepo.GetCurrentLiveAsync(symbol, ct);
        if (round == null) return false;
        if (await _betRepo.GetByTxHashAsync(txHash, ct) != null)
            return false;
        var position = side == "bull" ? Position.Bull : Position.Bear;
        var bet = new BetEntity
        {
            RoundId = round.Id,
            UserAddress = detail.In_Message?.Source ?? string.Empty,
            Amount = detail.Amount,
            Position = position,
            Claimed = false,
            Reward = 0m,
            TxHash = detail.Hash,
            Lt = detail.Lt,
            Status = BetStatus.Pending
        };
        await _betRepo.InsertAsync(bet);
        return true;
    }
}

/// <summary>
/// TonAPI 交易详情模型。
/// </summary>
/// <param name="Amount"></param>
/// <param name="In_Message"></param>
/// <param name="Hash"></param>
public record TonTxDetail(decimal Amount, InMsg? In_Message, string Hash)
{
    /// <summary>
    /// 账户逻辑时间。
    /// </summary>
    public ulong Lt { get; init; }
}

/// <summary>
/// 入站消息模型。
/// </summary>
/// <param name="Source"></param>
/// <param name="Comment"></param>
/// <param name="Destination"></param>
public record InMsg(string? Source, string? Comment, string? Destination);
