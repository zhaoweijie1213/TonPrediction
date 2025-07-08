using System.Threading;
using System.Threading.Tasks;
using QYQ.Base.Common.IOCExtensions;
using TonPrediction.Application.Database.Entities;

namespace TonPrediction.Application.Services.Interface;

/// <summary>
/// SignalR 推送服务接口。
/// </summary>
public interface IPredictionHubService : ITransientDependency
{
    /// <summary>
    /// 推送当前回合信息。
    /// </summary>
    /// <param name="round">当前回合实体。</param>
    /// <param name="currentPrice">最新价格。</param>
    Task PushCurrentRoundAsync(RoundEntity round, decimal currentPrice);

    /// <summary>
    /// 推送回合开始消息。
    /// </summary>
    /// <param name="roundId">回合唯一编号。</param>
    /// <param name="epoch">回合期次。</param>
    Task PushRoundStartedAsync(long roundId, long epoch);

    /// <summary>
    /// 推送回合锁定消息。
    /// </summary>
    /// <param name="roundId">回合唯一编号。</param>
    /// <param name="epoch">回合期次。</param>
    Task PushRoundLockedAsync(long roundId, long epoch);

    /// <summary>
    /// 推送结算开始消息。
    /// </summary>
    /// <param name="roundId">回合唯一编号。</param>
    /// <param name="epoch">回合期次。</param>
    Task PushSettlementStartedAsync(long roundId, long epoch);

    /// <summary>
    /// 推送回合结束消息。
    /// </summary>
    /// <param name="roundId">回合唯一编号。</param>
    /// <param name="epoch">回合期次。</param>
    Task PushRoundEndedAsync(long roundId, long epoch);

    /// <summary>
    /// 推送结算结束消息。
    /// </summary>
    /// <param name="roundId">回合唯一编号。</param>
    /// <param name="epoch">回合期次。</param>
    Task PushSettlementEndedAsync(long roundId, long epoch);
}
