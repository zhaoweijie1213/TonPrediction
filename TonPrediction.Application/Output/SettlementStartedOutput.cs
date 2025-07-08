namespace TonPrediction.Application.Output;

/// <summary>
/// 开始结算推送信息。
/// </summary>
public class SettlementStartedOutput
{
    /// <summary>
    /// 进入结算阶段的回合编号。
    /// </summary>
    public long RoundId { get; set; }
}
