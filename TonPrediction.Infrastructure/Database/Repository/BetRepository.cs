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
    public class BetRepository(ILogger<BetRepository> logger, IOptionsMonitor<DatabaseConfig> options, DbType dbType = DbType.MySql) : BaseRepository<BetEntity>(logger, options.CurrentValue.Default, dbType), IBetRepository
    {

    }
}
