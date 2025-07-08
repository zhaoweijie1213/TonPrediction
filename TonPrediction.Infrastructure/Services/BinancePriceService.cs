using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using TonPrediction.Application.Services;
using TonPrediction.Application.Services.Interface;

namespace TonPrediction.Infrastructure.Services
{
    /// <summary>
    /// 使用 Binance WebSocket 与 REST 获取价格。
    /// </summary>
    public class BinancePriceService(
        IHttpClientFactory httpClientFactory,
        ILogger<BinancePriceService> logger) : IPriceService, IDisposable
    {
        private readonly HttpClient _httpClient = httpClientFactory.CreateClient();
        private readonly ILogger<BinancePriceService> _logger = logger;
        private readonly ConcurrentDictionary<string, decimal> _prices = new();
        private readonly ConcurrentDictionary<string, ClientWebSocket> _sockets = new();
        private readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);

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
            if (!_prices.TryGetValue(pair, out var price))
            {
                price = await FetchRestAsync(pair);
                _prices[pair] = price;
                _ = EnsureWebSocketAsync(pair, ct);
            }

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
                    _ = Task.Run(() => ReceiveLoopAsync(pair, socket, ct), CancellationToken.None);
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
        private async Task ReceiveLoopAsync(string pair, ClientWebSocket socket, CancellationToken ct)
        {
            var buffer = new byte[1024];
            while (!ct.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                try
                {
                    var result = await socket.ReceiveAsync(buffer, ct);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, ct);
                        break;
                    }

                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var data = JsonSerializer.Deserialize<WsResponse>(json, _options);
                    if (data != null)
                    {
                        _prices[pair] = data.Price;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Binance WebSocket receive error");
                    break;
                }
            }
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
            foreach (var socket in _sockets.Values)
            {
                try
                {
                    socket.Abort();
                }
                catch
                {
                    // ignore
                }
            }
        }
    }
}
