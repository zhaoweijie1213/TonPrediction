namespace TonPrediction.Application.Output;

/// <summary>
/// 回合结束推送信息。
/// </summary>
public class RoundEndedOutput
{
    /// <summary>
    /// 已结束回合的编号。
    /// </summary>
    public long RoundId { get; set; }
}
