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
    /// 领奖记录仓库实现。
    /// </summary>
    /// <param name="logger">日志组件。</param>
    /// <param name="options">数据库配置。</param>
    /// <param name="dbType">数据库类型。</param>
    public class ClaimRepository(ILogger<ClaimRepository> logger, IOptionsMonitor<DatabaseConfig> options, DbType dbType = DbType.MySql)
        : BaseRepository<ClaimEntity>(logger, options.CurrentValue.Default, dbType), IClaimRepository
    {
    }
}
