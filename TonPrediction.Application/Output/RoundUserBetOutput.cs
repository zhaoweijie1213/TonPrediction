using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TonPrediction.Application.Enums;

namespace TonPrediction.Application.Output;

/// <summary>
/// 回合信息及用户下注详情。
/// </summary>
public class RoundUserBetOutput
{
    /// <summary>
    /// 回合唯一编号，用于业务请求。
    /// </summary>
    [JsonProperty("id")]
    public long Id { get; set; }

    /// <summary>
    /// 期次，从 1 开始递增。
    /// </summary>
    public long Epoch { get; set; }

    /// <summary>
    /// 锁定价格。
    /// </summary>
    public string LockPrice { get; set; } = string.Empty;

    /// <summary>
    /// 收盘价格。
    /// </summary>
    public string ClosePrice { get; set; } = string.Empty;

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
    /// 开始时间 Unix 秒。
    /// </summary>
    public long StartTime { get; set; }

    /// <summary>
    /// 结束时间 Unix 秒。
    /// </summary>
    public long EndTime { get; set; }

    /// <summary>
    /// 回合当前状态。
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public RoundStatus Status { get; set; }

    /// <summary>
    /// 看涨赔率。
    /// </summary>
    public string BullOdds { get; set; } = string.Empty;

    /// <summary>
    /// 看跌赔率。
    /// </summary>
    public string BearOdds { get; set; } = string.Empty;

    /// <summary>
    /// 回合结果，指出看涨或看跌获胜。
    /// </summary>
    public Position? WinnerSide { get; set; }

    /// <summary>
    /// 用户下注方向，若未下注则为 null。
    /// </summary>
    public Position? Position { get; set; }

    /// <summary>
    /// 用户下注金额，未下注则为 "0"。
    /// </summary>
    public string BetAmount { get; set; } = string.Empty;

    /// <summary>
    /// 奖励金额，未中奖为 "0"。
    /// </summary>
    public string Reward { get; set; } = string.Empty;

    /// <summary>
    /// 是否已领取奖励。
    /// </summary>
    public bool Claimed { get; set; }
}
