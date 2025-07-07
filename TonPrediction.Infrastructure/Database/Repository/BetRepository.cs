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
            string status,
            int page,
            int pageSize,
            CancellationToken ct = default)
        {
            var query = Db.Queryable<BetEntity>()
                .Where(b => b.UserAddress == address);
            query = status switch
            {
                "claimed" => query.Where(b => b.Claimed),
                "unclaimed" => query.Where(b => !b.Claimed),
                _ => query
            };
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
        public async Task<BetEntity?> GetByTxHashAsync(string txHash, CancellationToken ct = default)
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
    }
}
