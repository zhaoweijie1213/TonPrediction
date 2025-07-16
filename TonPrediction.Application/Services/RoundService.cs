using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using QYQ.Base.Common.ApiResult;
using SqlSugar;
using TonPrediction.Application.Config;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Enums;
using TonPrediction.Application.Extensions;
using TonPrediction.Application.Output;
using TonPrediction.Application.Services.Interface;

namespace TonPrediction.Application.Services;

/// <summary>
/// 回合信息查询业务实现。
/// </summary>
public class RoundService(IRoundRepository roundRepo, IBetRepository betRepo, IOptionsMonitor<PredictionConfig> predictionConfig) : IRoundService
{
    private readonly IRoundRepository _roundRepo = roundRepo;
    //private readonly IConfiguration _configuration = configuration;
    private readonly IBetRepository _betRepo = betRepo;

    /// <summary>
    /// 获取历史回合列表
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="limit"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<ApiResult<List<RoundHistoryOutput>>> GetHistoryAsync(
        string symbol = "ton",
        int limit = 3,
        CancellationToken ct = default)
    {
        var api = new ApiResult<List<RoundHistoryOutput>>();
        limit = limit is <= 0 or > 100 ? 3 : limit;
        var list = await _roundRepo.GetRoundsAsync(symbol, limit);
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
            BullOdds = r.BullAmount > 0 ? ((decimal)r.TotalAmount / r.BullAmount).ToAmountString() : "0",
            BearOdds = r.BearAmount > 0 ? ((decimal)r.TotalAmount / r.BearAmount).ToAmountString() : "0"
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
        var intervalSec = predictionConfig.CurrentValue.RoundIntervalSeconds;
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

    /// <summary>
    /// 获取最近回合及用户下注信息。
    /// </summary>
    /// <param name="address"></param>
    /// <param name="symbol"></param>
    /// <param name="limit"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<ApiResult<List<RoundUserBetOutput>>> GetRecentAsync(
        string? address,
        string symbol = "ton",
        int limit = 5,
        CancellationToken ct = default)
    {
        var api = new ApiResult<List<RoundUserBetOutput>>();
        limit = limit is <= 0 or > 100 ? 3 : limit;
        var rounds = await _roundRepo.GetRecentAsync(symbol, limit);
        var roundIds = rounds.Select(r => r.Id).ToArray();
        var bets = roundIds.Length == 0 || string.IsNullOrWhiteSpace(address)
            ? []
            : await _betRepo.GetByAddressAndRoundsAsync(address!.ToRawAddress(), roundIds, ct);
        var betMap = bets.ToDictionary(b => b.RoundId);

        var list = new List<RoundUserBetOutput>();
        foreach (var r in rounds)
        {
            betMap.TryGetValue(r.Id, out var bet);
            var item = new RoundUserBetOutput
            {
                Id = r.Id,
                Epoch = r.Epoch,
                LockPrice = r.LockPrice.ToAmountString(),
                ClosePrice = r.ClosePrice.ToAmountString(),
                TotalAmount = r.TotalAmount.ToAmountString(),
                BullAmount = r.BullAmount.ToAmountString(),
                BearAmount = r.BearAmount.ToAmountString(),
                RewardPool = r.RewardAmount.ToAmountString(),
                StartTime = new DateTimeOffset(r.StartTime).ToUnixTimeSeconds(),
                EndTime = new DateTimeOffset(r.CloseTime).ToUnixTimeSeconds(),
                Status = r.Status,
                BullOdds = r.BullAmount > 0 ? ((decimal)r.TotalAmount / r.BullAmount).ToAmountString() : "0",
                BearOdds = r.BearAmount > 0 ? ((decimal)r.TotalAmount / r.BearAmount).ToAmountString() : "0",
                WinnerSide = r.WinnerSide,
                Position = bet?.Position,
                BetAmount = bet?.Amount.ToAmountString() ?? "0",
                Reward = bet?.Reward.ToAmountString() ?? "0",
                Claimed = bet?.Claimed ?? false
            };
            list.Add(item);
        }
        list = list.OrderBy(x => x.Epoch).ToList();
        api.SetRsult(ApiResultCode.Success, list);
        return api;
    }
}
