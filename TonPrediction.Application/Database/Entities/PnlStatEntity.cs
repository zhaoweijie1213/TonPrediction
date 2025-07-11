using SqlSugar;

namespace TonPrediction.Application.Database.Entities;

/// <summary>
/// 用户盈亏统计记录。
/// </summary>
[SugarTable("pnl_stat")]
public class PnlStatEntity
{
    /// <summary>
    /// 编号
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 预测币种符号。
    /// </summary>
    [SugarColumn(ColumnName = "symbol", Length = 16)]
    public string Symbol { get; set; } = string.Empty;
    /// <summary>
    /// 用户地址。
    /// </summary>
    [SugarColumn(ColumnName = "user_address")]
    public string UserAddress { get; set; } = string.Empty;

    /// <summary>
    /// 累计下注金额。
    /// </summary>
    [SugarColumn(ColumnName = "total_bet", ColumnDataType = "bigint")]
    public long TotalBet { get; set; }

    /// <summary>
    /// 累计奖励金额。
    /// </summary>
    [SugarColumn(ColumnName = "total_reward", ColumnDataType = "bigint")]
    public long TotalReward { get; set; }

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
    [SugarColumn(ColumnName = "best_round_profit", ColumnDataType = "bigint")]
    public long BestRoundProfit { get; set; }
}
