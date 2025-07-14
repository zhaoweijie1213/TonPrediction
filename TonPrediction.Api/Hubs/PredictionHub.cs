using Microsoft.AspNetCore.SignalR;
using TonPrediction.Application.Extensions;

namespace TonPrediction.Api.Hubs
{
    /// <summary>
    /// 实时推送预测数据的 SignalR Hub。
    /// </summary>
    public class PredictionHub(ILogger<PredictionHub> logger, PresenceTracker presenceTracker) : Hub
    {
        /// <summary>
        /// 连接时不加入任何用户组。
        /// </summary>
        /// <returns>异步任务。</returns>
        public override async Task OnConnectedAsync()
        {
            var cid = Context.ConnectionId;                                     // 连接 ID
            var userId = Context.UserIdentifier;                                // 如果配置了  ClaimTypes.NameIdentifier
            var ip = Context.GetHttpContext()?.Connection.RemoteIpAddress;      // 客户端 IP
            var ua = Context.GetHttpContext()?.Request.Headers["User-Agent"].ToString();
            //var address = Context.GetHttpContext()?.Request.Query["address"];   // 你的业务参数

            logger.LogInformation("""
                新连接: {cid}, 用户:{userId}, IP:{ip}, UA:{ua}
                """, cid, userId, ip, ua);
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// 用户连接钱包后调用，加入对应地址组。
        /// </summary>
        /// <param name="address">钱包地址。</param>
        public async Task JoinAddressAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) return;
            logger.LogInformation("JoinAddressAsync.连接钱包地址:{address}", address.ToRawAddress());

            await Groups.AddToGroupAsync(Context.ConnectionId, address.ToRawAddress());

            presenceTracker.Add(Context.ConnectionId, address.ToRawAddress());
        }

        /// <summary>
        /// 断开连接时无需特殊处理，组成员会自动清理
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public override Task OnDisconnectedAsync(Exception? ex)
        {
            presenceTracker.Remove(Context.ConnectionId);
            return base.OnDisconnectedAsync(ex);
        }
    }
}
