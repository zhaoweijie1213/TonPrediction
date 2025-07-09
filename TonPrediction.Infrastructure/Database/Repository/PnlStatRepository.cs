using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QYQ.Base.SqlSugar;
using SqlSugar;
using TonPrediction.Application.Database.Config;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;

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
    public async Task<PnlStatEntity?> GetByAddressAsync(string address)
    {
        return await Db.Queryable<PnlStatEntity>()
            .Where(s => s.UserAddress == address)
            .FirstAsync();
    }

    /// <inheritdoc />
    public async Task<List<PnlStatEntity>> GetPagedAsync(string rankBy, int page, int pageSize)
    {
        var query = Db.Queryable<PnlStatEntity>();
        query = rankBy switch
        {
            "rounds" => query.OrderBy(s => s.Rounds, OrderByType.Desc),
            "totalBet" => query.OrderBy(s => s.TotalBet, OrderByType.Desc),
            "winRate" => query.OrderBy("IF(rounds>0, win_rounds/rounds,0) desc"),
            _ => query.OrderBy("(total_reward-total_bet) desc")
        };
        return await query.ToPageListAsync(page, pageSize);
    }

    /// <inheritdoc />
    public async Task<int> GetRankAsync(string address, string rankBy)
    {
        var stat = await GetByAddressAsync(address);
        if (stat == null) return 0;
        var query = Db.Queryable<PnlStatEntity>();
        return rankBy switch
        {
            "rounds" => await query.Where(s => s.Rounds > stat.Rounds).CountAsync() + 1,
            "totalBet" => await query.Where(s => s.TotalBet > stat.TotalBet).CountAsync() + 1,
            "winRate" => await query.Where("IF(rounds>0, win_rounds/rounds,0) > IF(@rounds>0,@win/@rounds,0)",
                new { rounds = stat.Rounds, win = stat.WinRounds }).CountAsync() + 1,
            _ => await query.Where(s => s.TotalReward - s.TotalBet > stat.TotalReward - stat.TotalBet).CountAsync() + 1
        };
    }
}
