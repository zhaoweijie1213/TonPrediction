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
        logger.LogDebug("PushCurrentRoundAsync.当前回合信息推送:{output}", JsonConvert.SerializeObject(output));
        return _hub.Clients.All.SendAsync("currentRound", output);
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
        logger.LogDebug("PushCurrentRoundAsync.当前回合信息推送:{output}", JsonConvert.SerializeObject(output));
        return _hub.Clients.All.SendAsync("roundStarted", output);
    }


    /// <summary>
    /// 锁定回合
    /// </summary>
    /// <param name="roundId">回合唯一编号。</param>
    /// <param name="epoch">回合期次。</param>
    /// <returns></returns>
    public Task PushRoundLockedAsync(long roundId, long epoch) =>
        _hub.Clients.All.SendAsync("roundLocked", new RoundLockedOutput { RoundId = roundId, Epoch = epoch });

    /// <summary>
    /// 推送开始结算消息
    /// </summary>
    /// <param name="roundId">回合唯一编号。</param>
    /// <param name="epoch">回合期次。</param>
    /// <returns></returns>
    public Task PushSettlementStartedAsync(long roundId, long epoch) =>
        _hub.Clients.All.SendAsync("settlementStarted", new SettlementStartedOutput { RoundId = roundId, Epoch = epoch });

    /// <summary>
    /// 回合结束
    /// </summary>
    /// <param name="roundId">回合唯一编号。</param>
    /// <param name="epoch">回合期次。</param>
    /// <returns></returns>
    public Task PushRoundEndedAsync(long roundId, long epoch) =>
        _hub.Clients.All.SendAsync("roundEnded", new RoundEndedOutput { RoundId = roundId, Epoch = epoch });

    /// <summary>
    /// 推送结算结束消息
    /// </summary>
    /// <param name="roundId">回合唯一编号。</param>
    /// <param name="epoch">回合期次。</param>
    /// <returns></returns>
    public Task PushSettlementEndedAsync(long roundId, long epoch) =>
        _hub.Clients.All.SendAsync("settlementEnded", new SettlementEndedOutput { RoundId = roundId, Epoch = epoch });
}
