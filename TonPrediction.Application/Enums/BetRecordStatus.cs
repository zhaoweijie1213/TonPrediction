namespace TonPrediction.Application.Enums;

/// <summary>
/// 下注记录筛选状态。
/// </summary>
public enum BetRecordStatus
{
    /// <summary>
    /// 全部记录。
    /// </summary>
    All = 0,

    /// <summary>
    /// 已领取奖励的记录。
    /// </summary>
    Claimed = 1,

    /// <summary>
    /// 未领取奖励的记录。
    /// </summary>
    Unclaimed = 2
}
