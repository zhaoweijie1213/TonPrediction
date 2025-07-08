using System.Text.Json.Serialization;

namespace TonPrediction.Application.Output;

/// <summary>
/// 回合锁定推送信息。
/// </summary>
public class RoundLockedOutput
{
    /// <summary>
    /// 锁定的回合唯一编号，用于业务请求。
    /// </summary>
    [JsonPropertyName("id")]
    public long RoundId { get; set; }

    /// <summary>
    /// 锁定的回合期次。
    /// </summary>
    public long Epoch { get; set; }
}
