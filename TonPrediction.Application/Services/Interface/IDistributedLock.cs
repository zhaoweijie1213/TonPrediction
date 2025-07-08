namespace TonPrediction.Application.Services.Interface;

using QYQ.Base.Common.IOCExtensions;

/// <summary>
/// 分布式锁服务接口。
/// </summary>
public interface IDistributedLock : ITransientDependency
{
    /// <summary>
    /// 尝试获取指定键的锁。
    /// </summary>
    /// <param name="key">锁键。</param>
    /// <param name="expiry">锁过期时间。</param>
    /// <returns>获取成功则返回释放锁的句柄，否则为 null。</returns>
    Task<IDisposable?> AcquireAsync(string key, TimeSpan expiry);
}
