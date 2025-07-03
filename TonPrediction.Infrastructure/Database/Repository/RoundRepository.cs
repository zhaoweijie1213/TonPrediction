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
    /// 回合仓库实现
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="connectionString"></param>
    /// <param name="dbType"></param>
    public class RoundRepository(ILogger<RoundRepository> logger, IOptionsMonitor<DatabaseConfig> options, DbType dbType = DbType.MySql) : BaseRepository<RoundEntity>(logger, options.CurrentValue.Default, dbType), IRoundRepository
    {

    }
}
