using TonPrediction.Application.Enums;

namespace TonPrediction.Application.Output;

/// <summary>
/// 单个下注记录信息。
/// </summary>
public class BetRecordOutput
{
    /// <summary>
    /// 回合编号。
    /// </summary>
    public long RoundId { get; set; }

    /// <summary>
    /// 下注方向。
    /// </summary>
    public Position Position { get; set; }

    /// <summary>
    /// 下注金额。
    /// </summary>
    public string Amount { get; set; } = string.Empty;

    /// <summary>
    /// 锁定价格。
    /// </summary>
    public string LockPrice { get; set; } = string.Empty;

    /// <summary>
    /// 收盘价格。
    /// </summary>
    public string ClosePrice { get; set; } = string.Empty;

    /// <summary>
    /// 奖励金额。
    /// </summary>
    public string Reward { get; set; } = string.Empty;

    /// <summary>
    /// 是否已领取。
    /// </summary>
    public bool Claimed { get; set; }

    /// <summary>
    /// 结果。
    /// </summary>
    public BetResult Result { get; set; }
}
