namespace TonPrediction.Application.Enums;

/// <summary>
/// 领奖交易状态。
/// </summary>
public enum ClaimStatus
{
    /// <summary>
    /// 交易已提交待确认。
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 交易已确认。
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// 交易失败。
    /// </summary>
    Failed = 2
}
