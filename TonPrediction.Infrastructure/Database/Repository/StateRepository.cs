using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QYQ.Base.SqlSugar;
using SqlSugar;
using TonPrediction.Application.Database.Config;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;

namespace TonPrediction.Infrastructure.Database.Repository;

/// <summary>
/// 应用状态仓库实现。
/// </summary>
/// <param name="logger">日志组件。</param>
/// <param name="options">数据库配置。</param>
/// <param name="dbType">数据库类型。</param>
public class StateRepository(
    ILogger<StateRepository> logger,
    IOptionsMonitor<DatabaseConfig> options,
    DbType dbType = DbType.MySql)
    : BaseRepository<StateEntity>(logger, options.CurrentValue.Default, dbType),
        IStateRepository
{
    /// <inheritdoc />
    public async Task<string?> GetValueAsync(string key)
    {
        var entity = await Db.Queryable<StateEntity>()
            .Where(s => s.Key == key)
            .FirstAsync();
        return entity?.Value;
    }

    /// <inheritdoc />
    public async Task SetValueAsync(string key, string value)
    {
        var exist = await Db.Queryable<StateEntity>()
            .Where(s => s.Key == key)
            .FirstAsync();
        if (exist == null)
        {
            await Db.Insertable(new StateEntity { Key = key, Value = value })
                .ExecuteCommandAsync();
        }
        else
        {
            exist.Value = value;
            await Db.Updateable(exist).ExecuteCommandAsync();
        }
    }
}
