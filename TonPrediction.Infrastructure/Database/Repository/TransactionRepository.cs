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
    /// 交易记录仓库实现。
    /// </summary>
    /// <param name="logger">日志组件。</param>
    /// <param name="options">数据库配置。</param>
    /// <param name="dbType">数据库类型。</param>
    public class TransactionRepository(ILogger<TransactionRepository> logger, IOptionsMonitor<DatabaseConfig> options, DbType dbType = DbType.MySql)
        : BaseRepository<TransactionEntity>(logger, options.CurrentValue.Default, dbType), ITransactionRepository
    {
    }
}
