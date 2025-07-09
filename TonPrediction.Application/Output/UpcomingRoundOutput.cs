using System.Text.Json.Serialization;

namespace TonPrediction.Application.Output;

/// <summary>
/// 即将开始的回合时间信息。
/// </summary>
public class UpcomingRoundOutput
{
    /// <summary>
    /// 回合唯一编号（预计），部分回合可能为 0。
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// 回合期次（预计）。
    /// </summary>
    public long Epoch { get; set; }

    /// <summary>
    /// 开始时间 Unix 秒。
    /// </summary>
    public long StartTime { get; set; }

    /// <summary>
    /// 结束时间 Unix 秒。
    /// </summary>
    public long EndTime { get; set; }
}
