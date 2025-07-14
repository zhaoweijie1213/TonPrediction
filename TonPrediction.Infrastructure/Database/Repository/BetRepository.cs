using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QYQ.Base.SqlSugar;
using SqlSugar;
using TonPrediction.Application.Database.Config;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using System.Linq.Expressions;

namespace TonPrediction.Infrastructure.Database.Repository
{
    /// <summary>
    /// 下注仓库实现
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="connectionString"></param>
    /// <param name="dbType"></param>
    public class BetRepository(
        ILogger<BetRepository> logger,
        IOptionsMonitor<DatabaseConfig> options,
        DbType dbType = DbType.MySql)
        : BaseRepository<BetEntity>(logger, options.CurrentValue.Default, dbType),
            IBetRepository
    {
        /// <inheritdoc />
        public async Task<List<BetEntity>> GetPagedByAddressAsync(
            string address,
            Expression<Func<BetEntity, bool>>? predicate,
            int page,
            int pageSize,
            CancellationToken ct = default)
        {
            var query = Db.Queryable<BetEntity>()
                .Where(b => b.UserAddress == address);
            if (predicate != null)
                query = query.Where(predicate);
            return await query.OrderBy(b => b.Id, OrderByType.Desc)
                .ToPageListAsync(page, pageSize);
        }

        /// <inheritdoc />
        public async Task<List<BetEntity>> GetByAddressAsync(
            string address,
            CancellationToken ct = default)
        {
            return await Db.Queryable<BetEntity>()
                .Where(b => b.UserAddress == address)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<List<BetEntity>> GetByRoundAsync(
            long roundId,
            CancellationToken ct = default)
        {
            return await Db.Queryable<BetEntity>()
                .Where(b => b.RoundId == roundId)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<BetEntity?> GetByTxHashAsync(string txHash)
        {
            return await Db.Queryable<BetEntity>()
                .Where(b => b.TxHash == txHash)
                .FirstAsync();
        }

        /// <inheritdoc />
        public async Task<BetEntity?> GetByAddressAndRoundAsync(
            string address,
            long roundId,
            CancellationToken ct = default)
        {
            return await Db.Queryable<BetEntity>()
                .Where(b => b.UserAddress == address && b.RoundId == roundId)
                .FirstAsync();
        }

        /// <inheritdoc />
        public async Task<List<BetEntity>> GetByAddressAndRoundsAsync(
            string address,
            long[] roundIds,
            CancellationToken ct = default)
        {
            return await Db.Queryable<BetEntity>()
                .Where(b => b.UserAddress == address && roundIds.Contains(b.RoundId))
                .ToListAsync();
        }

        /// <summary>
        /// 获取指定回合和用户地址的下注信息。
        /// </summary>
        /// <param name="roundId"></param>
        /// <param name="userAddress"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<BetEntity> GetByRoundAndUserAsync(long roundId, string userAddress)
        {
            return Db.Queryable<BetEntity>()
                .Where(b => b.RoundId == roundId && b.UserAddress == userAddress)
                .FirstAsync();
        }
    }
}
