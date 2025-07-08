using System.Text.Json.Serialization;

namespace TonPrediction.Application.Output;

/// <summary>
/// 开始结算推送信息。
/// </summary>
public class SettlementStartedOutput
{
    /// <summary>
    /// 进入结算阶段的回合唯一编号，用于业务请求。
    /// </summary>
    [JsonPropertyName("id")]
    public long RoundId { get; set; }

    /// <summary>
    /// 进入结算阶段的回合期次。
    /// </summary>
    public long Epoch { get; set; }
}
