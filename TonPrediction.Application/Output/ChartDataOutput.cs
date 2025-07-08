using TonPrediction.Application.Enums;

namespace TonPrediction.Application.Output;

/// <summary>
/// 价格走势图推送信息。
/// </summary>
public class ChartDataOutput
{
    /// <summary>
    /// 时间戳数组（Unix 秒，升序）。
    /// </summary>
    public long[] Timestamps { get; set; } = Array.Empty<long>();

    /// <summary>
    /// 价格数组，对应每个时间点的价格。
    /// </summary>
    public string[] Prices { get; set; } = Array.Empty<string>();
}
