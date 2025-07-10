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
    public long Id { get; set; }

    /// <summary>
    /// 总下注金额。
    /// </summary>
    public string TotalAmount { get; set; } = string.Empty;

    /// <summary>
    /// 押看涨金额。
    /// </summary>
    public string BullAmount { get; set; } = string.Empty;

    /// <summary>
    /// 押看跌金额。
    /// </summary>
    public string BearAmount { get; set; } = string.Empty;

    /// <summary>
    /// 奖池金额，扣除手续费后可分配的奖励。
    /// </summary>
    public string RewardPool { get; set; } = string.Empty;

    /// <summary>
    /// 看涨赔率。
    /// </summary>
    public string BullOdds { get; set; } = string.Empty;

    /// <summary>
    /// 看跌赔率。
    /// </summary>
    public string BearOdds { get; set; } = string.Empty;
}
