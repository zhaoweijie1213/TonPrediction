using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using TonPrediction.Api.Hubs;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Output;
using TonPrediction.Application.Services.Interface;

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
            LockPrice = round.LockPrice.ToString("F8"),
            CurrentPrice = currentPrice.ToString("F8"),
            TotalAmount = round.TotalAmount.ToString("F8"),
            BullAmount = round.BullAmount.ToString("F8"),
            BearAmount = round.BearAmount.ToString("F8"),
            RewardPool = round.RewardAmount.ToString("F8"),
            EndTime = new DateTimeOffset(round.CloseTime).ToUnixTimeSeconds(),
            BullOdds = oddsBull.ToString("F8"),
            BearOdds = oddsBear.ToString("F8"),
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
