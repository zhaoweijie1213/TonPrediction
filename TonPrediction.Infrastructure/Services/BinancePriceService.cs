using Binance.Net.Clients;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TonPrediction.Application.Services;
using TonPrediction.Application.Services.Interface;

namespace TonPrediction.Infrastructure.Services
{
    /// <summary>
    /// 使用 Binance WebSocket 与 REST 获取价格。
    /// </summary>
    public class BinancePriceService(
        IHttpClientFactory httpClientFactory,
        ILogger<BinancePriceService> logger) : IPriceService, IDisposable, IAsyncDisposable
    {
        private readonly HttpClient _httpClient = httpClientFactory.CreateClient();
        private readonly ILogger<BinancePriceService> _logger = logger;
        //private readonly ConcurrentDictionary<string, decimal> _prices = new();
        private readonly ConcurrentDictionary<string, ClientWebSocket> _sockets = new();
        private readonly ConcurrentDictionary<string, Task> _tasks = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

        /// <summary>
        /// 币安 REST 客户端，用于获取价格数据。
        /// </summary>
        private readonly BinanceRestClient _restClient = new();

        /// <summary>
        /// 获取指定币种对的价格。
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="vsCurrency"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<PriceResult> GetAsync(
            string symbol,
            string vsCurrency = "usd",
            CancellationToken ct = default)
        {
            // Binance 不支持直接使用 "USD" 交易对，需转换为 "USDT"
            var currency = vsCurrency.Equals("usd", StringComparison.OrdinalIgnoreCase)
                ? "usdt"
                : vsCurrency;
            var pair = (symbol + currency).ToUpperInvariant();
            var tickerResult = await _restClient.SpotApi.ExchangeData.GetTickerAsync(pair);
            decimal price = tickerResult.Data.LastPrice;

            //decimal price = await FetchRestAsync(pair);

            //var linkedToken = CancellationTokenSource
            //    .CreateLinkedTokenSource(ct, _cts.Token).Token;
            //_ = EnsureWebSocketAsync(pair, linkedToken);


            return new PriceResult(symbol, vsCurrency, price, DateTimeOffset.UtcNow);
        }

        /// <summary>
        /// 拉取 Binance REST API 获取价格。
        /// </summary>
        /// <param name="pair"></param>
        /// <returns></returns>
        private async Task<decimal> FetchRestAsync(string pair)
        {
            var url = $"https://api.binance.com/api/v3/ticker/price?symbol={pair}";
            try
            {
                var resp = await _httpClient.GetFromJsonAsync<RestResponse>(url);
                return resp?.Price ?? 0m;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch price from Binance REST");
                return 0m;
            }
        }

        /// <summary>
        /// websocket 连接 Binance 获取实时价格。
        /// </summary>
        /// <param name="pair"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task EnsureWebSocketAsync(string pair, CancellationToken ct)
        {
            if (_sockets.ContainsKey(pair))
                return;

            try
            {
                var socket = new ClientWebSocket();
                await socket.ConnectAsync(new Uri($"wss://stream.binance.com/ws/{pair.ToLower()}@trade"), ct);
                if (_sockets.TryAdd(pair, socket))
                {
                    var task = Task.Run(() => ReceiveLoopAsync(pair, socket), CancellationToken.None);
                    _tasks[pair] = task;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect Binance WebSocket");
            }
        }

        /// <summary>
        /// 接收 Binance WebSocket 消息循环。
        /// </summary>
        /// <param name="pair"></param>
        /// <param name="socket"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async IAsyncEnumerable<decimal> ReceiveLoopAsync(string pair, ClientWebSocket socket)
        {
            var buffer = new byte[1024];
            List<decimal> txs = [];
            while (socket.State == WebSocketState.Open)
            {
                try
                {
                    var result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                        break;
                    }
                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var data = JsonSerializer.Deserialize<WsResponse>(json, _options);
                    if (data != null)
                    {
                        txs.Add(data.Price);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Binance WebSocket receive error");
                    break;
                }

                foreach (var tx in txs)
                {
                    yield return tx;
                }
            }



            _tasks.TryRemove(pair, out _);
            _sockets.TryRemove(pair, out _);
        }

        /// <summary>
        /// 价格响应模型，用于 REST API 响应。
        /// </summary>
        private sealed class RestResponse
        {
            [JsonPropertyName("price")]
            public decimal Price { get; set; }
        }

        /// <summary>
        /// websocket 响应模型，用于实时价格更新。
        /// </summary>
        private sealed class WsResponse
        {
            [JsonPropertyName("p")]
            public decimal Price { get; set; }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            _cts.Cancel();
            try
            {
                await Task.WhenAll(_tasks.Values);
            }
            catch
            {
                // ignore
            }

            foreach (var socket in _sockets.Values)
            {
                try
                {
                    if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    }
                }
                catch
                {
                    // ignore
                }
                finally
                {
                    socket.Dispose();
                }
            }
        }
    }
}
