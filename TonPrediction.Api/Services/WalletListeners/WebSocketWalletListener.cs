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
public class WebSocketWalletListener(IHttpClientFactory httpFactory, ILogger<WebSocketWalletListener> logger,IOptionsMonitor<TonConfig> tonConfig) : IWalletListener
{
    private readonly HttpClient _http = httpFactory.CreateClient("TonApi");
    private readonly ILogger<WebSocketWalletListener> _logger = logger;
    private const string WsUrlTemplate = "/v2/ws/accounts/transactions?accounts={0}";

    /// <inheritdoc />
    public async IAsyncEnumerable<TonTxDetail> ListenAsync(string walletAddress, ulong lastLt, [EnumeratorCancellation] CancellationToken ct)
    {
        var wsBaseUri = new Uri(tonConfig.CurrentValue.WebSocketUrl);
        var wsUri = new Uri(wsBaseUri, string.Format(WsUrlTemplate, walletAddress));
        using var ws = new ClientWebSocket();
        await ws.ConnectAsync(wsUri, ct);
        var buffer = new byte[4096];
        while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            var items = new List<TonTxDetail>();
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                break;
            }
            var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
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
                _logger.LogError(ex, "WS parse error");
            }

            foreach (var tx in items)
            {
                yield return tx;
            }
        }
    }
}
