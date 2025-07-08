using System.Text.Json.Serialization;

namespace TonPrediction.Application.Output;

/// <summary>
/// 回合结束推送信息。
/// </summary>
public class RoundEndedOutput
{
    /// <summary>
    /// 已结束回合的唯一编号，用于业务请求。
    /// </summary>
    [JsonPropertyName("id")]
    public long RoundId { get; set; }

    /// <summary>
    /// 已结束回合的期次。
    /// </summary>
    public long Epoch { get; set; }
}
