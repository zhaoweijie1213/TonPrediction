using SqlSugar;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Enums;
using TonPrediction.Application.Output;
using TonPrediction.Application.Services.Interface;

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
    public async Task<List<BetRecordOutput>> GetRecordsAsync(
        string address,
        string status = "all",
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize is <= 0 or > 100 ? 10 : pageSize;
        var bets = await _betRepo.GetPagedByAddressAsync(address, status, page, pageSize);
        var roundIds = bets.Select(b => b.RoundId).ToArray();
        var rounds = await _roundRepo.GetByRoundIdsAsync(roundIds);
        var map = rounds.ToDictionary(r => r.Epoch);
        var list = new List<BetRecordOutput>();
        foreach (var bet in bets)
        {
            if (!map.TryGetValue(bet.RoundId, out var round))
                continue;
            var result = BetResult.Draw;
            if (round.ClosePrice > round.LockPrice)
                result = bet.Position == Position.Bull ? BetResult.Win : BetResult.Lose;
            else if (round.ClosePrice < round.LockPrice)
                result = bet.Position == Position.Bear ? BetResult.Win : BetResult.Lose;
            var output = new BetRecordOutput
            {
                RoundId = bet.RoundId,
                Epoch = round.Epoch,
                Position = bet.Position,
                Amount = bet.Amount.ToString("F8"),
                LockPrice = round.LockPrice.ToString("F8"),
                ClosePrice = round.ClosePrice.ToString("F8"),
                Reward = bet.Reward.ToString("F8"),
                Claimed = bet.Claimed,
                TxHash = bet.TxHash,
                Result = result
            };
            list.Add(output);
        }
        return list;
    }

    /// <inheritdoc />
    public async Task<PnlOutput> GetPnlAsync(string address)
    {
        var bets = await _betRepo.GetByAddressAsync(address);
        var totalBet = bets.Sum(b => b.Amount);
        var totalReward = bets.Sum(b => b.Reward);
        var rounds = bets.Count;
        var winRounds = bets.Count(b => b.Reward > 0m);
        return new PnlOutput
        {
            TotalBet = totalBet.ToString("F8"),
            TotalReward = totalReward.ToString("F8"),
            NetProfit = (totalReward - totalBet).ToString("F8"),
            Rounds = rounds,
            WinRounds = winRounds
        };
    }
}
