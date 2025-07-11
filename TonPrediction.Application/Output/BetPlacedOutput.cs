using System.Text.Json.Serialization;

namespace TonPrediction.Application.Output;

/// <summary>
/// 下注成功推送内容。
/// </summary>
public class BetPlacedOutput
{
    /// <summary>
    /// 回合唯一编号，用于业务请求。
    /// </summary>
    [JsonPropertyName("id")]
    public long RoundId { get; set; }

    /// <summary>
    /// 回合期次。
    /// </summary>
    public long Epoch { get; set; }

    /// <summary>
    /// 下注金额。
    /// </summary>
    public string Amount { get; set; } = string.Empty;

    /// <summary>
    /// 下注交易哈希。
    /// </summary>
    public string TxHash { get; set; } = string.Empty;
}
