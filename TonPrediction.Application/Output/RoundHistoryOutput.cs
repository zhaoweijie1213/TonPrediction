using TonPrediction.Application.Enums;

namespace TonPrediction.Application.Output;

/// <summary>
/// 历史回合信息。
/// </summary>
public class RoundHistoryOutput
{
    /// <summary>
    /// 回合编号。
    /// </summary>
    public long RoundId { get; set; }

    /// <summary>
    /// 锁定价格。
    /// </summary>
    public string LockPrice { get; set; } = string.Empty;

    /// <summary>
    /// 收盘价格。
    /// </summary>
    public string ClosePrice { get; set; } = string.Empty;

    /// <summary>
    /// 总下注金额。
    /// </summary>
    public string TotalAmount { get; set; } = string.Empty;

    /// <summary>
    /// 押看涨金额。
    /// </summary>
    public string BullAmount { get; set; } = string.Empty;

    /// <summary>
    /// 押看跌金额。
    /// </summary>
    public string BearAmount { get; set; } = string.Empty;

    /// <summary>
    /// 奖金池。
    /// </summary>
    public string RewardPool { get; set; } = string.Empty;

    /// <summary>
    /// 结束时间 Unix 秒。
    /// </summary>
    public long EndTime { get; set; }

    /// <summary>
    /// 看涨赔率。
    /// </summary>
    public string BullOdds { get; set; } = string.Empty;

    /// <summary>
    /// 看跌赔率。
    /// </summary>
    public string BearOdds { get; set; } = string.Empty;
}
