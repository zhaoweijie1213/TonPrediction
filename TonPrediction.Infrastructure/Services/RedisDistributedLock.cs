using EasyCaching.Core;
using EasyCaching.Redis;
using StackExchange.Redis;
using TonPrediction.Application.Services.Interface;

namespace TonPrediction.Infrastructure.Services;

/// <summary>
/// 基于 Redis 的分布式锁实现。
/// </summary>
public class RedisDistributedLock(IEnumerable<IRedisDatabaseProvider> redisDatabaseProviders) : IDistributedLock
{
    /// <summary>
    /// 获取redis 数据库
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public IDatabase GetDatabase(string name = "DefaultRedis")
    {
        var _dbProvider = redisDatabaseProviders.First(x => x.DBProviderName.Equals(name));

        return _dbProvider.GetDatabase();
    }


    /// <summary>
    /// redis 分布式锁实现
    /// </summary>
    /// <param name="key"></param>
    /// <param name="expiry"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<IDisposable?> AcquireAsync(
        string key,
        TimeSpan expiry,
        CancellationToken ct = default)
    {
        var db = GetDatabase();
        var token = Guid.NewGuid().ToString();
        var acquired = await db.StringSetAsync(key, token, expiry, When.NotExists);
        return acquired ? new Releaser(db, key, token) : null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="db"></param>
    /// <param name="key"></param>
    /// <param name="token"></param>
    private sealed class Releaser(IDatabase db, string key, string token) : IDisposable
    {
        private readonly IDatabase _db = db;
        private readonly string _key = key;
        private readonly string _token = token;
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            var value = _db.StringGet(_key);
            if (value == _token)
            {
                _db.KeyDelete(_key);
            }

            _disposed = true;
        }
    }
}
