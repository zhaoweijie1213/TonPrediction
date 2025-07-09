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

    /// <summary>
    /// 失利回合数。
    /// </summary>
    public int LoseRounds { get; set; }

    /// <summary>
    /// 胜率（0-1）。
    /// </summary>
    public string WinRate { get; set; } = string.Empty;

    /// <summary>
    /// 回合平均投入。
    /// </summary>
    public string AverageBet { get; set; } = string.Empty;

    /// <summary>
    /// 回合平均收益。
    /// </summary>
    public string AverageReturn { get; set; } = string.Empty;

    /// <summary>
    /// 最佳回合编号。
    /// </summary>
    public long BestRoundId { get; set; }

    /// <summary>
    /// 最佳回合收益。
    /// </summary>
    public string BestRoundProfit { get; set; } = string.Empty;
}
