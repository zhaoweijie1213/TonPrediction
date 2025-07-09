using SqlSugar;

namespace TonPrediction.Application.Database.Entities;

/// <summary>
/// 用户盈亏统计记录。
/// </summary>
[SugarTable("pnl_stat")]
public class PnlStatEntity
{
    /// <summary>
    /// 预测币种符号，主键之一。
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, ColumnName = "symbol", Length = 16)]
    public string Symbol { get; set; } = string.Empty;
    /// <summary>
    /// 用户地址，主键。
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, ColumnName = "user_address")]
    public string UserAddress { get; set; } = string.Empty;

    /// <summary>
    /// 累计下注金额。
    /// </summary>
    [SugarColumn(ColumnName = "total_bet", ColumnDataType = "decimal(18,8)")]
    public decimal TotalBet { get; set; }

    /// <summary>
    /// 累计奖励金额。
    /// </summary>
    [SugarColumn(ColumnName = "total_reward", ColumnDataType = "decimal(18,8)")]
    public decimal TotalReward { get; set; }

    /// <summary>
    /// 参与回合数。
    /// </summary>
    [SugarColumn(ColumnName = "rounds")]
    public int Rounds { get; set; }

    /// <summary>
    /// 获胜回合数。
    /// </summary>
    [SugarColumn(ColumnName = "win_rounds")]
    public int WinRounds { get; set; }

    /// <summary>
    /// 最佳回合编号。
    /// </summary>
    [SugarColumn(ColumnName = "best_round_id")]
    public long BestRoundId { get; set; }

    /// <summary>
    /// 最佳回合收益。
    /// </summary>
    [SugarColumn(ColumnName = "best_round_profit", ColumnDataType = "decimal(18,8)")]
    public decimal BestRoundProfit { get; set; }
}
