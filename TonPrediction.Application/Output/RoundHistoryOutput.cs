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
    /// 押涨金额。
    /// </summary>
    public string UpAmount { get; set; } = string.Empty;

    /// <summary>
    /// 押跌金额。
    /// </summary>
    public string DownAmount { get; set; } = string.Empty;

    /// <summary>
    /// 奖金池。
    /// </summary>
    public string RewardPool { get; set; } = string.Empty;

    /// <summary>
    /// 结束时间 Unix 秒。
    /// </summary>
    public long EndTime { get; set; }

    /// <summary>
    /// 上涨赔率。
    /// </summary>
    public string OddsUp { get; set; } = string.Empty;

    /// <summary>
    /// 下跌赔率。
    /// </summary>
    public string OddsDown { get; set; } = string.Empty;
}
