using SqlSugar;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Enums;
using TonPrediction.Application.Output;
using TonPrediction.Application.Services.Interface;
using TonPrediction.Application.Extensions;
using QYQ.Base.Common.ApiResult;
using System.Linq;

namespace TonPrediction.Application.Services;

/// <summary>
/// 预测记录查询业务实现。
/// </summary>
public class PredictionService(
    IBetRepository betRepo,
    IRoundRepository roundRepo) : IPredictionService
{
    private readonly IBetRepository _betRepo = betRepo;
    private readonly IRoundRepository _roundRepo = roundRepo;

    /// <summary>
    /// 获取用户的投注记录
    /// </summary>
    /// <param name="address"></param>
    /// <param name="status"></param>
    /// <param name="page"></param>
    /// <param name="pageSize"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<ApiResult<List<RoundUserBetOutput>>> GetRecordsAsync(
        string address,
        string status = "all",
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var api = new ApiResult<List<RoundUserBetOutput>>();
        page = page <= 0 ? 1 : page;
        pageSize = pageSize is <= 0 or > 100 ? 10 : pageSize;
        var bets = await _betRepo.GetPagedByAddressAsync(address, status, page, pageSize);
        var roundIds = bets.Select(b => b.RoundId).ToArray();
        var rounds = await _roundRepo.GetByRoundIdsAsync(roundIds);
        var map = rounds.ToDictionary(r => r.Epoch);
        var list = new List<RoundUserBetOutput>();
        foreach (var bet in bets)
        {
            if (!map.TryGetValue(bet.RoundId, out var round))
                continue;
            var result = BetResult.Draw;
            if (round.ClosePrice > round.LockPrice)
                result = bet.Position == Position.Bull ? BetResult.Win : BetResult.Lose;
            else if (round.ClosePrice < round.LockPrice)
                result = bet.Position == Position.Bear ? BetResult.Win : BetResult.Lose;
            var output = new RoundUserBetOutput
            {
                RoundId = bet.RoundId,
                Epoch = round.Epoch,
                LockPrice = round.LockPrice.ToAmountString(),
                ClosePrice = round.ClosePrice.ToAmountString(),
                TotalAmount = round.TotalAmount.ToAmountString(),
                BullAmount = round.BullAmount.ToAmountString(),
                BearAmount = round.BearAmount.ToAmountString(),
                RewardPool = round.RewardAmount.ToAmountString(),
                StartTime = new DateTimeOffset(round.StartTime).ToUnixTimeSeconds(),
                EndTime = new DateTimeOffset(round.CloseTime).ToUnixTimeSeconds(),
                BullOdds = round.BullAmount > 0m ? (round.TotalAmount / round.BullAmount).ToAmountString() : "0",
                BearOdds = round.BearAmount > 0m ? (round.TotalAmount / round.BearAmount).ToAmountString() : "0",
                Position = bet.Position,
                BetAmount = bet.Amount.ToAmountString(),
                Reward = bet.Reward.ToAmountString(),
                Claimed = bet.Claimed
            };
            list.Add(output);
        }
        api.SetRsult(ApiResultCode.Success, list);
        return api;
    }

    /// <inheritdoc />
    public async Task<ApiResult<PnlOutput>> GetPnlAsync(string address)
    {
        var api = new ApiResult<PnlOutput>();
        var bets = await _betRepo.GetByAddressAsync(address);
        var totalBet = bets.Sum(b => b.Amount);
        var totalReward = bets.Sum(b => b.Reward);
        var rounds = bets.Count;
        var winRounds = bets.Count(b => b.Reward > 0m);
        var loseRounds = rounds - winRounds;
        var netProfit = totalReward - totalBet;
        var winRate = rounds > 0
            ? ((decimal)winRounds / rounds).ToAmountString()
            : "0";
        var avgBet = rounds > 0 ? (totalBet / rounds).ToAmountString() : "0";
        var avgReturn = rounds > 0 ? (totalReward / rounds).ToAmountString() : "0";
        var best = bets.OrderByDescending(b => b.Reward - b.Amount).FirstOrDefault();
        var bestId = best?.RoundId ?? 0;
        var bestProfit = best != null ? (best.Reward - best.Amount).ToAmountString() : "0";
        var output = new PnlOutput
        {
            TotalBet = totalBet.ToAmountString(),
            TotalReward = totalReward.ToAmountString(),
            NetProfit = netProfit.ToAmountString(),
            Rounds = rounds,
            WinRounds = winRounds,
            LoseRounds = loseRounds,
            WinRate = winRate,
            AverageBet = avgBet,
            AverageReturn = avgReturn,
            BestRoundId = bestId,
            BestRoundProfit = bestProfit
        };
        api.SetRsult(ApiResultCode.Success, output);
        return api;
    }
}
