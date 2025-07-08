namespace TonPrediction.Application.Output;

/// <summary>
/// 结算结束推送信息。
/// </summary>
public class SettlementEndedOutput
{
    /// <summary>
    /// 已结算回合编号。
    /// </summary>
    public long RoundId { get; set; }
}
