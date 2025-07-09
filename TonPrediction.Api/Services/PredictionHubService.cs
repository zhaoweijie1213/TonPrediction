using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using TonPrediction.Api.Hubs;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Output;
using TonPrediction.Application.Services.Interface;
using TonPrediction.Application.Extensions;

namespace TonPrediction.Api.Services;

/// <summary>
/// SignalR 推送实现。
/// </summary>
public class PredictionHubService(ILogger<PredictionHubService> logger, IHubContext<PredictionHub> hub) : IPredictionHubService
{
    private readonly IHubContext<PredictionHub> _hub = hub;

    /// <summary>
    /// 当前回合信息推送
    /// </summary>
    /// <param name="round"></param>
    /// <param name="currentPrice"></param>
    /// <returns></returns>
    public Task PushCurrentRoundAsync(RoundEntity round, decimal currentPrice)
    {
        var oddsBull = round.BullAmount > 0m ? round.TotalAmount / round.BullAmount : 0m;
        var oddsBear = round.BearAmount > 0m ? round.TotalAmount / round.BearAmount : 0m;
        var output = new CurrentRoundOutput
        {
            RoundId = round.Id,
            Epoch = round.Epoch,
            LockPrice = round.LockPrice.ToAmountString(),
            CurrentPrice = currentPrice.ToAmountString(),
            TotalAmount = round.TotalAmount.ToAmountString(),
            BullAmount = round.BullAmount.ToAmountString(),
            BearAmount = round.BearAmount.ToAmountString(),
            RewardPool = round.RewardAmount.ToAmountString(),
            EndTime = new DateTimeOffset(round.CloseTime).ToUnixTimeSeconds(),
            BullOdds = oddsBull.ToAmountString(),
            BearOdds = oddsBear.ToAmountString(),
            Status = round.Status
        };
        logger.LogInformation("PushCurrentRoundAsync.当前回合信息推送:{output}", JsonConvert.SerializeObject(output));
        return _hub.Clients.All.SendAsync("currentRound", output);
    }


    /// <summary>
    /// 下个回合奖池信息推送。
    /// </summary>
    /// <param name="round">回合实体。</param>
    /// <param name="currentPrice">当前价格（忽略）。</param>
    /// <returns></returns>
    public Task PushNextRoundAsync(RoundEntity round, decimal currentPrice)
    {
        var oddsBull = round.BullAmount > 0m ? round.TotalAmount / round.BullAmount : 0m;
        var oddsBear = round.BearAmount > 0m ? round.TotalAmount / round.BearAmount : 0m;
        var output = new NextRoundOutput
        {
            RoundId = round.Id,
            RewardPool = round.RewardAmount.ToAmountString(),
            Symbol = round.Symbol,
            CurrentPrice = currentPrice.ToAmountString(),
            BullOdds = oddsBull.ToAmountString(),
            BearOdds = oddsBear.ToAmountString()
        };
        logger.LogInformation("PushNextRoundAsync.下个回合奖池推送:{output}", JsonConvert.SerializeObject(output));
        return _hub.Clients.All.SendAsync("nextRound", output);
    }

    /// <summary>
    /// 回合开始
    /// </summary>
    /// <param name="roundId">回合唯一编号。</param>
    /// <param name="epoch">回合期次。</param>
    /// <returns></returns>
    public Task PushRoundStartedAsync(long roundId, long epoch)
    {
        var output = new RoundStartedOutput { RoundId = roundId, Epoch = epoch };
        logger.LogInformation("PushRoundStartedAsync.回合开始推送:{output}", JsonConvert.SerializeObject(output));
        return _hub.Clients.All.SendAsync("roundStarted", output);
    }


    /// <summary>
    /// 锁定回合
    /// </summary>
    /// <param name="roundId">回合唯一编号。</param>
    /// <param name="epoch">回合期次。</param>
    /// <returns></returns>
    public Task PushRoundLockedAsync(long roundId, long epoch)
    {
        var output = new RoundLockedOutput { RoundId = roundId, Epoch = epoch };
        logger.LogInformation("PushRoundLockedAsync.锁定回合推送:{output}", JsonConvert.SerializeObject(output));
        return _hub.Clients.All.SendAsync("roundLocked", output);
    }
       

    /// <summary>
    /// 推送开始结算消息
    /// </summary>
    /// <param name="roundId">回合唯一编号。</param>
    /// <param name="epoch">回合期次。</param>
    /// <returns></returns>
    public Task PushSettlementStartedAsync(long roundId, long epoch)
    {
        var output = new SettlementStartedOutput { RoundId = roundId, Epoch = epoch };
        logger.LogInformation("PushSettlementStartedAsync.推送开始结算消息:{output}", JsonConvert.SerializeObject(output));
        return _hub.Clients.All.SendAsync("settlementStarted", output);
    }
       

    /// <summary>
    /// 回合结束
    /// </summary>
    /// <param name="roundId">回合唯一编号。</param>
    /// <param name="epoch">回合期次。</param>
    /// <returns></returns>
    public Task PushRoundEndedAsync(long roundId, long epoch)
    {
        var output = new RoundEndedOutput { RoundId = roundId, Epoch = epoch };
        logger.LogInformation("PushRoundEndedAsync.推送回合结束:{output}", JsonConvert.SerializeObject(output));
        return _hub.Clients.All.SendAsync("roundEnded", output);
    }


    /// <summary>
    /// 推送结算结束消息
    /// </summary>
    /// <param name="roundId">回合唯一编号。</param>
    /// <param name="epoch">回合期次。</param>
    /// <returns></returns>
    public Task PushSettlementEndedAsync(long roundId, long epoch)
    {
        var output = new SettlementEndedOutput { RoundId = roundId, Epoch = epoch };
        logger.LogInformation("PushSettlementEndedAsync.推送结算结束消息:{output}", JsonConvert.SerializeObject(output));
        return _hub.Clients.All.SendAsync("settlementEnded", output);
    }
}
