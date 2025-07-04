namespace TonPrediction.Application.Output;

/// <summary>
/// 即将开始的回合时间信息。
/// </summary>
public class UpcomingRoundOutput
{
    /// <summary>
    /// 回合编号（预计）。
    /// </summary>
    public long RoundId { get; set; }

    /// <summary>
    /// 开始时间 Unix 秒。
    /// </summary>
    public long StartTime { get; set; }

    /// <summary>
    /// 结束时间 Unix 秒。
    /// </summary>
    public long EndTime { get; set; }
}
