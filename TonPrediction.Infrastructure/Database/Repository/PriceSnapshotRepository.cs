using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QYQ.Base.SqlSugar;
using SqlSugar;
using TonPrediction.Application.Database.Config;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;

namespace TonPrediction.Infrastructure.Database.Repository
{
    /// <summary>
    /// 价格快照仓库实现。
    /// </summary>
    /// <param name="logger">日志组件。</param>
    /// <param name="options">数据库配置。</param>
    /// <param name="dbType">数据库类型。</param>
    public class PriceSnapshotRepository(
        ILogger<PriceSnapshotRepository> logger,
        IOptionsMonitor<DatabaseConfig> options,
        DbType dbType = DbType.MySql)
        : BaseRepository<PriceSnapshotEntity>(logger, options.CurrentValue.Default, dbType),
            IPriceSnapshotRepository
    {
        /// <inheritdoc />
        public async Task<List<PriceSnapshotEntity>> GetSinceAsync(
            string symbol,
            DateTime since,
            CancellationToken ct = default)
        {
            return await Db.Queryable<PriceSnapshotEntity>()
                .Where(p => p.Symbol == symbol && p.Timestamp >= since)
                .OrderBy(p => p.Timestamp, OrderByType.Asc)
                .ToListAsync();
        }
    }
}
