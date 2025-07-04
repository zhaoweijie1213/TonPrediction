using Microsoft.AspNetCore.Mvc;
using QYQ.Base.Common.ApiResult;
using SqlSugar;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Enums;
using TonPrediction.Application.Output;

namespace TonPrediction.Api.Controllers;

/// <summary>
/// 下注记录与盈亏接口。
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PredictionsController(IBetRepository betRepo, IRoundRepository roundRepo) : ControllerBase
{
    private readonly IBetRepository _betRepo = betRepo;
    private readonly IRoundRepository _roundRepo = roundRepo;

    /// <summary>
    /// 分页获取下注记录。
    /// </summary>
    [HttpGet("round")]
    public async Task<ApiResult<List<BetRecordOutput>>> GetRoundAsync(
        [FromQuery] string address,
        [FromQuery] string status = "all",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize is <= 0 or > 100 ? 10 : pageSize;
        dynamic betDyn = _betRepo;
        ISqlSugarClient db = betDyn.Db;
        dynamic betQuery = db.Queryable<BetEntity>()
            .Where("user_address = @address", new { address });
        betQuery = status switch
        {
            "claimed" => betQuery.Where("claimed = true"),
            "unclaimed" => betQuery.Where("claimed = false"),
            _ => betQuery
        };
        var bets = (List<BetEntity>)await betQuery
            .OrderBy("id", OrderByType.Desc)
            .ToPageListAsync(page, pageSize);
        var ids = bets.Select(b => b.Epoch).ToArray();
        var rounds = (List<RoundEntity>)await db.Queryable<RoundEntity>()
            .In(ids)
            .ToListAsync();
        var map = rounds.ToDictionary(r => r.Id);
        var list = new List<BetRecordOutput>();
        foreach (var bet in bets)
        {
            if (!map.TryGetValue(bet.Epoch, out var round))
                continue;
            var result = BetResult.Draw;
            if (round.ClosePrice > round.LockPrice)
                result = bet.Position == Position.Bull ? BetResult.Win : BetResult.Lose;
            else if (round.ClosePrice < round.LockPrice)
                result = bet.Position == Position.Bear ? BetResult.Win : BetResult.Lose;
            var output = new BetRecordOutput
            {
                RoundId = bet.Epoch,
                Position = bet.Position,
                Amount = bet.Amount.ToString("F8"),
                LockPrice = round.LockPrice.ToString("F8"),
                ClosePrice = round.ClosePrice.ToString("F8"),
                Reward = bet.Reward.ToString("F8"),
                Claimed = bet.Claimed,
                Result = result
            };
            list.Add(output);
        }
        var api = new ApiResult<List<BetRecordOutput>>();
        api.SetRsult(ApiResultCode.Success, list);
        return api;
    }

    /// <summary>
    /// 获取盈亏汇总。
    /// </summary>
    [HttpGet("pnl")]
    public async Task<ApiResult<PnlOutput>> GetPnlAsync([FromQuery] string address)
    {
        dynamic betDyn = _betRepo;
        ISqlSugarClient db = betDyn.Db;
        var bets = (List<BetEntity>)await db.Queryable<BetEntity>()
            .Where("user_address = @address", new { address })
            .ToListAsync();
        var totalBet = bets.Sum(b => b.Amount);
        var totalReward = bets.Sum(b => b.Reward);
        var rounds = bets.Count;
        var winRounds = bets.Count(b => b.Reward > 0m);
        var output = new PnlOutput
        {
            TotalBet = totalBet.ToString("F8"),
            TotalReward = totalReward.ToString("F8"),
            NetProfit = (totalReward - totalBet).ToString("F8"),
            Rounds = rounds,
            WinRounds = winRounds
        };
        var api = new ApiResult<PnlOutput>();
        api.SetRsult(ApiResultCode.Success, output);
        return api;
    }
}
