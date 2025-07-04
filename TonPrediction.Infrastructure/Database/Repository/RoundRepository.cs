using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QYQ.Base.SqlSugar;
using SqlSugar;
using TonPrediction.Application.Database.Config;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Enums;

namespace TonPrediction.Infrastructure.Database.Repository
{
    /// <summary>
    /// 回合仓库实现
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="connectionString"></param>
    /// <param name="dbType"></param>
    public class RoundRepository(
        ILogger<RoundRepository> logger,
        IOptionsMonitor<DatabaseConfig> options,
        DbType dbType = DbType.MySql)
        : BaseRepository<RoundEntity>(logger, options.CurrentValue.Default, dbType),
            IRoundRepository
    {
        /// <inheritdoc />
        public async Task<RoundEntity?> GetLatestAsync(
            string symbol,
            CancellationToken ct = default)
        {
            return await Db.Queryable<RoundEntity>()
                .Where(r => r.Symbol == symbol)
                .OrderBy(r => r.Id, OrderByType.Desc)
                .FirstAsync();
        }

        /// <inheritdoc />
        public async Task<RoundEntity?> GetCurrentLiveAsync(
            string symbol,
            CancellationToken ct = default)
        {
            return await Db.Queryable<RoundEntity>()
                .Where(r => r.Symbol == symbol && r.Status == RoundStatus.Live)
                .OrderBy(r => r.Id, OrderByType.Desc)
                .FirstAsync();
        }

        /// <inheritdoc />
        public async Task<List<RoundEntity>> GetEndedAsync(
            string symbol,
            int limit,
            CancellationToken ct = default)
        {
            return await Db.Queryable<RoundEntity>()
                .Where(r => r.Symbol == symbol && r.Status == RoundStatus.Ended)
                .OrderBy(r => r.Id, OrderByType.Desc)
                .Take(limit)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<List<RoundEntity>> GetByIdsAsync(long[] ids, CancellationToken ct = default)
        {
            return await Db.Queryable<RoundEntity>()
                .Where(r => ids.Contains(r.Id))
                .ToListAsync();
        }
    }
}
