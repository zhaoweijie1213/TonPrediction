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
        public async Task<RoundEntity?> GetLatestAsync(string symbol)
        {
            return await Db.Queryable<RoundEntity>()
                .Where(r => r.Symbol == symbol)
                .OrderBy(r => r.Epoch, OrderByType.Desc)
                .FirstAsync();
        }

        /// <inheritdoc />
        public async Task<RoundEntity?> GetCurrentLiveAsync(string symbol)
        {
            return await Db.Queryable<RoundEntity>()
                .Where(r => r.Symbol == symbol && r.Status == RoundStatus.Betting)
                .OrderBy(r => r.Epoch, OrderByType.Desc)
                .FirstAsync();
        }

        /// <inheritdoc />
        public async Task<RoundEntity?> GetUpcomingAsync(string symbol)
        {
            return await Db.Queryable<RoundEntity>()
                .Where(r => r.Symbol == symbol && r.Status == RoundStatus.Upcoming)
                .OrderBy(r => r.Epoch, OrderByType.Asc)
                .FirstAsync();
        }

        /// <inheritdoc />
        public async Task<RoundEntity?> GetCurrentLockedAsync(string symbol)
        {
            return await Db.Queryable<RoundEntity>()
                .Where(r => r.Symbol == symbol && r.Status == RoundStatus.Live)
                .OrderBy(r => r.Epoch, OrderByType.Desc)
                .FirstAsync();
        }

        /// <inheritdoc />
        public async Task<List<RoundEntity>> GetEndedAsync(string symbol, int limit)
        {
            return await Db.Queryable<RoundEntity>()
                .Where(r => r.Symbol == symbol && r.Status == RoundStatus.Completed)
                .OrderBy(r => r.Epoch, OrderByType.Desc)
                .Take(limit)
                .ToListAsync();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="roundIds"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<List<RoundEntity>> GetByRoundIdsAsync(long[] roundIds)
        {
            return await Db.Queryable<RoundEntity>()
                .Where(r => roundIds.Contains(r.Id))
                .ToListAsync();
        }
    }
}
