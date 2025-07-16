namespace TonPrediction.Application.Enums;

/// <summary>
/// 下注状态。
/// </summary>
public enum BetStatus
{
    /// <summary>
    /// 待确认，用户上报但链上未监听到。
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 已确认，后台监听到链上交易。
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// 失败或超时。
    /// </summary>
    Failed = 2
}
