namespace TonPrediction.Application.Output;

/// <summary>
/// 回合开始推送信息。
/// </summary>
public class RoundStartedOutput
{
    /// <summary>
    /// 新回合编号。
    /// </summary>
    public long RoundId { get; set; }
}
