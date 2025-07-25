using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QYQ.Base.SqlSugar;
using SqlSugar;
using TonPrediction.Application.Database.Config;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Enums;

namespace TonPrediction.Infrastructure.Database.Repository;

/// <summary>
/// 用户盈亏统计仓库实现。
/// </summary>
/// <param name="logger">日志组件。</param>
/// <param name="options">数据库配置。</param>
/// <param name="dbType">数据库类型。</param>
public class PnlStatRepository(
    ILogger<PnlStatRepository> logger,
    IOptionsMonitor<DatabaseConfig> options,
    DbType dbType = DbType.MySql)
    : BaseRepository<PnlStatEntity>(logger, options.CurrentValue.Default, dbType),
        IPnlStatRepository
{
    /// <inheritdoc />
    public async Task<PnlStatEntity?> GetByAddressAsync(string symbol, string address)
    {
        return await Db.Queryable<PnlStatEntity>()
            .Where(s => s.Symbol == symbol && s.UserAddress == address)
            .FirstAsync();
    }

    /// <inheritdoc />
    public async Task<List<PnlStatEntity>> GetPagedAsync(string symbol, RankByType rankBy, int page, int pageSize)
    {
        var query = Db.Queryable<PnlStatEntity>().Where(s => s.Symbol == symbol);
        query = rankBy switch
        {
            RankByType.Rounds => query.OrderBy(s => s.Rounds, OrderByType.Desc),
            RankByType.TotalBet => query.OrderBy(s => s.TotalBet, OrderByType.Desc),
            RankByType.WinRate => query.OrderBy("IF(rounds>0, win_rounds/rounds,0) desc"),
            RankByType.NetProfit => query.OrderBy("(total_reward-total_bet) desc"),
            _ => query.OrderBy(s => s.TotalReward, OrderByType.Desc),
        };
        return await query.ToPageListAsync(page, pageSize);
    }

    /// <inheritdoc />
    public async Task<int> GetRankAsync(string symbol, string address, RankByType rankBy)
    {
        var stat = await GetByAddressAsync(symbol, address);
        if (stat == null) return 0;
        var query = Db.Queryable<PnlStatEntity>().Where(s => s.Symbol == symbol);
        return rankBy switch
        {
            RankByType.Rounds => await query.Where(s => s.Rounds > stat.Rounds).CountAsync() + 1,
            RankByType.TotalBet => await query.Where(s => s.TotalBet > stat.TotalBet).CountAsync() + 1,
            RankByType.WinRate => await query.Where("IF(rounds>0, win_rounds/rounds,0) > IF(@rounds>0,@win/@rounds,0)", new { rounds = stat.Rounds, win = stat.WinRounds }).CountAsync() + 1,
            RankByType.NetProfit => await query.Where(s => s.TotalReward - s.TotalBet > stat.TotalReward - stat.TotalBet).CountAsync() + 1,
            _ => await query.Where(s => s.TotalReward > stat.TotalReward).CountAsync() + 1
        };
    }
}
