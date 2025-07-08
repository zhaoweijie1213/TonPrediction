using TonPrediction.Application.Database.Entities;
using QYQ.Base.SqlSugar;
using QYQ.Base.Common.IOCExtensions;

namespace TonPrediction.Application.Database.Repository;

/// <summary>
/// 应用状态仓库接口。
/// </summary>
public interface IStateRepository : IBaseRepository<StateEntity>, ITransientDependency
{
    /// <summary>
    /// 根据键获取值。
    /// </summary>
    /// <param name="key">键。</param>
    /// <returns>值或 null。</returns>
    Task<string?> GetValueAsync(string key);

    /// <summary>
    /// 设置键值。
    /// </summary>
    /// <param name="key">键。</param>
    /// <param name="value">值。</param>
    Task SetValueAsync(string key, string value);
}
