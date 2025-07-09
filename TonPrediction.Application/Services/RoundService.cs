using Microsoft.Extensions.Configuration;
using SqlSugar;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Enums;
using TonPrediction.Application.Output;
using TonPrediction.Application.Services.Interface;
using TonPrediction.Application.Extensions;
using QYQ.Base.Common.ApiResult;

namespace TonPrediction.Application.Services;

/// <summary>
/// 回合信息查询业务实现。
/// </summary>
public class RoundService(
    IRoundRepository roundRepo,
    IConfiguration configuration,
    IBetRepository betRepo) : IRoundService
{
    private readonly IRoundRepository _roundRepo = roundRepo;
    private readonly IConfiguration _configuration = configuration;
    private readonly IBetRepository _betRepo = betRepo;

    /// <inheritdoc />
    public async Task<ApiResult<List<RoundHistoryOutput>>> GetHistoryAsync(
        string symbol = "ton",
        int limit = 3,
        CancellationToken ct = default)
    {
        var api = new ApiResult<List<RoundHistoryOutput>>();
        limit = limit is <= 0 or > 100 ? 3 : limit;
        var list = await _roundRepo.GetEndedAsync(symbol, limit);
        var result = list.Select(r => new RoundHistoryOutput
        {
            RoundId = r.Id,
            Epoch = r.Epoch,
            LockPrice = r.LockPrice.ToAmountString(),
            ClosePrice = r.ClosePrice.ToAmountString(),
            TotalAmount = r.TotalAmount.ToAmountString(),
            BullAmount = r.BullAmount.ToAmountString(),
            BearAmount = r.BearAmount.ToAmountString(),
            RewardPool = r.RewardAmount.ToAmountString(),
            EndTime = new DateTimeOffset(r.CloseTime).ToUnixTimeSeconds(),
            BullOdds = r.BullAmount > 0m ? (r.TotalAmount / r.BullAmount).ToAmountString() : "0",
            BearOdds = r.BearAmount > 0m ? (r.TotalAmount / r.BearAmount).ToAmountString() : "0"
        }).ToList();
        api.SetRsult(ApiResultCode.Success, result);
        return api;
    }

    /// <summary>
    /// 获取下一回合信息
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="ct"></param>
    /// <returns>包含下一回合信息的对象。</returns>
    public async Task<ApiResult<UpcomingRoundOutput>> GetUpcomingAsync(
        string symbol = "ton",
        CancellationToken ct = default)
    {
        var api = new ApiResult<UpcomingRoundOutput>();
        var upcoming = await _roundRepo.GetCurrentBettingAsync(symbol);
        if (upcoming != null)
        {
            var result = new UpcomingRoundOutput
            {
                Id = upcoming.Id,
                Epoch = upcoming.Epoch,
                StartTime = new DateTimeOffset(upcoming.StartTime).ToUnixTimeSeconds(),
                EndTime = new DateTimeOffset(upcoming.CloseTime).ToUnixTimeSeconds()
            };
            api.SetRsult(ApiResultCode.Success, result);
            return api;
        }

        var latest = await _roundRepo.GetLatestAsync(symbol);
        var intervalSec = _configuration.GetValue<int>("ENV_ROUND_INTERVAL_SEC", 300);
        var startTime = latest?.CloseTime ?? DateTime.UtcNow;
        var startEpoch = (latest?.Epoch ?? 0) + 1;
        var fallback = new UpcomingRoundOutput
        {
            Id = 0,
            Epoch = startEpoch,
            StartTime = new DateTimeOffset(startTime).ToUnixTimeSeconds(),
            EndTime = new DateTimeOffset(startTime.AddSeconds(intervalSec)).ToUnixTimeSeconds()
        };
        api.SetRsult(ApiResultCode.Success, fallback);
        return api;
    }

    /// <inheritdoc />
    public async Task<ApiResult<List<RoundUserBetOutput>>> GetRecentAsync(
        string address,
        string symbol = "ton",
        int limit = 3,
        CancellationToken ct = default)
    {
        var api = new ApiResult<List<RoundUserBetOutput>>();
        limit = limit is <= 0 or > 100 ? 3 : limit;
        var rounds = await _roundRepo.GetRecentAsync(symbol, limit);
        var roundIds = rounds.Select(r => r.Id).ToArray();
        var bets = roundIds.Length == 0
            ? new List<BetEntity>()
            : await _betRepo.GetByAddressAndRoundsAsync(address, roundIds, ct);
        var betMap = bets.ToDictionary(b => b.RoundId);

        var list = new List<RoundUserBetOutput>();
        foreach (var r in rounds)
        {
            betMap.TryGetValue(r.Id, out var bet);
            var item = new RoundUserBetOutput
            {
                RoundId = r.Id,
                Epoch = r.Epoch,
                LockPrice = r.LockPrice.ToAmountString(),
                ClosePrice = r.ClosePrice.ToAmountString(),
                TotalAmount = r.TotalAmount.ToAmountString(),
                BullAmount = r.BullAmount.ToAmountString(),
                BearAmount = r.BearAmount.ToAmountString(),
                RewardPool = r.RewardAmount.ToAmountString(),
                StartTime = new DateTimeOffset(r.StartTime).ToUnixTimeSeconds(),
                EndTime = new DateTimeOffset(r.CloseTime).ToUnixTimeSeconds(),
                BullOdds = r.BullAmount > 0m ? (r.TotalAmount / r.BullAmount).ToAmountString() : "0",
                BearOdds = r.BearAmount > 0m ? (r.TotalAmount / r.BearAmount).ToAmountString() : "0",
                Position = bet?.Position,
                BetAmount = bet?.Amount.ToAmountString() ?? "0",
                Reward = bet?.Reward.ToAmountString() ?? "0",
                Claimed = bet?.Claimed ?? false
            };
            list.Add(item);
        }

        api.SetRsult(ApiResultCode.Success, list);
        return api;
    }
}
