using System.Text.Json.Serialization;

namespace TonPrediction.Application.Output;

/// <summary>
/// 下个回合奖池推送信息。
/// </summary>
public class NextRoundOutput
{
    /// <summary>
    /// 回合唯一编号。
    /// </summary>
    [JsonPropertyName("id")]
    public long RoundId { get; set; }

    /// <summary>
    /// 奖池金额，扣除手续费后可分配的奖励。
    /// </summary>
    public string RewardPool { get; set; } = string.Empty;

    /// <summary>
    /// 预测币种符号，如 ton、btc、eth。
    /// </summary>
    public string Symbol { get; set; } = string.Empty;
}
