using TonPrediction.Application.Enums;

namespace TonPrediction.Application.Output;

/// <summary>
/// 领奖结果。
/// </summary>
public class ClaimOutput
{
    /// <summary>
    /// 交易哈希。
    /// </summary>
    public string TxHash { get; set; } = string.Empty;

    /// <summary>
    /// 账户逻辑时间。
    /// </summary>
    public ulong Lt { get; set; }

    /// <summary>
    /// 交易状态。
    /// </summary>
    public ClaimStatus Status { get; set; }

    /// <summary>
    /// 交易时间（Unix 秒）。
    /// </summary>
    public long Timestamp { get; set; }
}
