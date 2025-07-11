using System.Net.Http;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using TonPrediction.Application.Config;
using TonPrediction.Application.Services;
using TonPrediction.Application.Services.Interface;

namespace TonPrediction.Api.Services.WalletListeners;

/// <summary>
/// 通过 WebSocket 订阅交易的监听实现。
/// </summary>
public class WebSocketWalletListener(IHttpClientFactory httpFactory, ILogger<WebSocketWalletListener> logger, IOptionsMonitor<TonConfig> tonConfig) : IWalletListener
{
    private readonly HttpClient _http = httpFactory.CreateClient("TonApi");
    // WebSocket 相对路径，基于 TonConfig.WebSocketUrl 构建
    //private const string WsUrlTemplate = "accounts/transactions?accounts={0}";

    /// <summary>
    /// 监听钱包地址的交易详情。
    /// </summary>
    /// <param name="walletAddress"></param>
    /// <param name="lastLt"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async IAsyncEnumerable<TonTxDetail> ListenAsync(string walletAddress, ulong lastLt, [EnumeratorCancellation] CancellationToken ct)
    {
        var apiKey = tonConfig.CurrentValue.ApiKey;
        var wsBaseUri = new Uri($"{tonConfig.CurrentValue.WebSocketUrl}?token={apiKey}");
        using var ws = new ClientWebSocket();
        await ws.ConnectAsync(wsBaseUri, ct);

        //发送订阅指令
        var sub = new
        {
            id = 1,
            jsonrpc = "2.0",
            method = "subscribe_account",
            @params = new[] { walletAddress }          // 可追加 ;operations=TonTransfer
        };

        var json = JsonConvert.SerializeObject(sub);
        await ws.SendAsync(Encoding.UTF8.GetBytes(json),
                           WebSocketMessageType.Text, true, ct);

        //接收推送
        var buffer = new byte[8 * 1024];
        while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            var items = new List<TonTxDetail>();
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                break;
            }
            var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
            logger.LogDebug("ListenAsync.接收websocket消息:{text}", text);
            try
            {
                var head = JsonConvert.DeserializeObject<SseTxHead>(text);
                if (head != null)
                {
                    var detail = await _http.GetFromJsonAsync<TonTxDetail>($"/v2/blockchain/transactions/{head.Tx_Hash}", ct);
                    if (detail != null)
                    {
                        items.Add(detail with { Hash = head.Tx_Hash, Lt = head.Lt });
                        lastLt = head.Lt;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "WS parse error");
            }

            foreach (var tx in items)
            {
                yield return tx;
            }
        }
    }
}
