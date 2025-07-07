using StackExchange.Redis;
using TonPrediction.Application.Services.Interface;

namespace TonPrediction.Infrastructure.Services;

/// <summary>
/// 基于 Redis 的分布式锁实现。
/// </summary>
public class RedisDistributedLock(IConnectionMultiplexer connection) : IDistributedLock
{
    private readonly IConnectionMultiplexer _connection = connection;

    /// <inheritdoc />
    public async Task<IDisposable?> AcquireAsync(
        string key,
        TimeSpan expiry,
        CancellationToken ct = default)
    {
        var db = _connection.GetDatabase();
        var token = Guid.NewGuid().ToString();
        var acquired = await db.StringSetAsync(key, token, expiry, When.NotExists);
        return acquired ? new Releaser(db, key, token) : null;
    }

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
