using Microsoft.AspNetCore.SignalR;

namespace TonPrediction.Api.Hubs
{
    /// <summary>
    /// 实时推送预测数据的 SignalR Hub。
    /// </summary>
    public class PredictionHub : Hub
    {
        /// <summary>
        /// 连接时不加入任何用户组。
        /// </summary>
        /// <returns>异步任务。</returns>
        public override Task OnConnectedAsync() => base.OnConnectedAsync();

        /// <summary>
        /// 用户连接钱包后调用，加入对应地址组。
        /// </summary>
        /// <param name="address">钱包地址。</param>
        public async Task JoinAddressAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) return;
            await Groups.AddToGroupAsync(Context.ConnectionId, address);
        }

        // 断开连接时无需特殊处理，组成员会自动清理
    }
}
