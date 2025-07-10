using System.Text.Json.Serialization;
using TonPrediction.Application.Enums;

namespace TonPrediction.Application.Output;

/// <summary>
/// 当前回合推送信息。
/// </summary>
public class CurrentRoundOutput
{
    /// <summary>
    /// 回合唯一编号，用于业务请求。
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// 币种
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// 最新价格。
    /// </summary>
    public string CurrentPrice { get; set; } = string.Empty;

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
    /// 奖金池。
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
