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
    IRoundRepository roundRepo,
    IPnlStatRepository statRepo) : IPredictionService
{
    private readonly IBetRepository _betRepo = betRepo;
    private readonly IRoundRepository _roundRepo = roundRepo;
    private readonly IPnlStatRepository _statRepo = statRepo;

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
                Status = round.Status,
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
    public async Task<ApiResult<PnlOutput>> GetPnlAsync(string symbol, string address)
    {
        var api = new ApiResult<PnlOutput>();
        var stat = await _statRepo.GetByAddressAsync(symbol, address);
        if (stat == null)
        {
            api.SetRsult(ApiResultCode.Success, new PnlOutput());
            return api;
        }

        var totalBet = stat.TotalBet;
        var totalReward = stat.TotalReward;
        var rounds = stat.Rounds;
        var winRounds = stat.WinRounds;
        var loseRounds = rounds - winRounds;
        var netProfit = totalReward - totalBet;
        var winRate = rounds > 0 ? ((decimal)winRounds / rounds).ToAmountString() : "0";
        var avgBet = rounds > 0 ? (totalBet / rounds).ToAmountString() : "0";
        var avgReturn = rounds > 0 ? (totalReward / rounds).ToAmountString() : "0";
        var bestId = stat.BestRoundId;
        var bestProfit = stat.BestRoundProfit.ToAmountString();
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
