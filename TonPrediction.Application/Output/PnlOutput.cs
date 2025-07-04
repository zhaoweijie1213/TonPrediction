namespace TonPrediction.Application.Output;

/// <summary>
/// 盈亏汇总信息。
/// </summary>
public class PnlOutput
{
    /// <summary>
    /// 总下注金额。
    /// </summary>
    public string TotalBet { get; set; } = string.Empty;

    /// <summary>
    /// 总奖励金额。
    /// </summary>
    public string TotalReward { get; set; } = string.Empty;

    /// <summary>
    /// 净收益 = 总奖励 - 总下注。
    /// </summary>
    public string NetProfit { get; set; } = string.Empty;

    /// <summary>
    /// 参与回合数。
    /// </summary>
    public int Rounds { get; set; }

    /// <summary>
    /// 获胜回合数。
    /// </summary>
    public int WinRounds { get; set; }
}
