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

        /// <inheritdoc />
        public async Task<PriceResult> GetAsync(
            string symbol,
            string vsCurrency = "usd",
            CancellationToken ct = default)
        {
            var pair = (symbol + vsCurrency).ToUpperInvariant();
            if (!_prices.TryGetValue(pair, out var price))
            {
                price = await FetchRestAsync(pair, ct);
                _prices[pair] = price;
                _ = EnsureWebSocketAsync(pair, ct);
            }

            return new PriceResult(symbol, vsCurrency, price, DateTimeOffset.UtcNow);
        }

        private async Task<decimal> FetchRestAsync(string pair, CancellationToken ct)
        {
            var url = $"https://api.binance.com/api/v3/ticker/price?symbol={pair}";
            try
            {
                var resp = await _httpClient.GetFromJsonAsync<RestResponse>(url, ct);
                return resp?.Price ?? 0m;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch price from Binance REST");
                return 0m;
            }
        }

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
                    _ = Task.Run(() => ReceiveLoopAsync(pair, socket, ct));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect Binance WebSocket");
            }
        }

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

        private sealed class RestResponse
        {
            [JsonPropertyName("price")]
            public decimal Price { get; set; }
        }

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
