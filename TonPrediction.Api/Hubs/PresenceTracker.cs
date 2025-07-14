using QYQ.Base.Common.IOCExtensions;
using StackExchange.Redis;
using System.Collections.Concurrent;

namespace TonPrediction.Api.Hubs
{
    /// <summary>
    /// 连接跟踪器，用于管理 SignalR 连接的在线状态。
    /// </summary>
    public class PresenceTracker : ISingletonDependency
    {
        private readonly ConcurrentDictionary<string, ClientInfo> _clients = new();

        /// <summary>
        /// 新增一个连接信息到跟踪器中。
        /// </summary>
        /// <param name="cid"></param>
        /// <param name="address"></param>
        public void Add(string cid, string address)
            => _clients[cid] = new ClientInfo(cid, address);

        /// <summary>
        /// 删除跟踪器中的连接信息。
        /// </summary>
        /// <param name="cid"></param>
        public void Remove(string cid)
            => _clients.TryRemove(cid, out _);

        /// <summary>
        /// 取某地址（组）下全部连接
        /// </summary>
        /// <param name="addressRaw"></param>
        /// <returns></returns>
        public IReadOnlyCollection<ClientInfo> ForAddress(string addressRaw)
            => _clients.Values.Where(c => c.Address == addressRaw).ToList();
    }

    /// <summary>
    /// 客户端信息
    /// </summary>
    /// <param name="ConnectionId"></param>
    /// <param name="Address"></param>
    public record ClientInfo(string ConnectionId, string Address);
}
