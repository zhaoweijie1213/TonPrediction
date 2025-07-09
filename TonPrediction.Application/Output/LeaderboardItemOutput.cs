namespace TonPrediction.Application.Output;

/// <summary>
/// 排行榜条目。
/// </summary>
public class LeaderboardItemOutput
{
    /// <summary>
    /// 排名次序。
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// 用户地址。
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// 参与回合数。
    /// </summary>
    public int Rounds { get; set; }

    /// <summary>
    /// 获胜回合数。
    /// </summary>
    public int WinRounds { get; set; }

    /// <summary>
    /// 失利回合数。
    /// </summary>
    public int LoseRounds { get; set; }

    /// <summary>
    /// 胜率。
    /// </summary>
    public string WinRate { get; set; } = string.Empty;

    /// <summary>
    /// 累计下注金额。
    /// </summary>
    public string TotalBet { get; set; } = string.Empty;

    /// <summary>
    /// 累计奖励金额。
    /// </summary>
    public string TotalReward { get; set; } = string.Empty;

    /// <summary>
    /// 净收益。
    /// </summary>
    public string NetProfit { get; set; } = string.Empty;
}
