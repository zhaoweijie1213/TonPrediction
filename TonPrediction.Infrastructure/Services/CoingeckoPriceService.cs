using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using TonPrediction.Application.Services;
using TonPrediction.Application.Services.Interface;

namespace TonPrediction.Infrastructure.Services
{
    /// <summary>
    /// 从 CoinGecko 获取价格。
    /// </summary>
    public class CoingeckoPriceService(
        IHttpClientFactory httpClientFactory,
        ILogger<CoingeckoPriceService> logger) : IPriceService
    {
        private readonly HttpClient _httpClient = httpClientFactory.CreateClient();
        private readonly ILogger<CoingeckoPriceService> _logger = logger;
        private const string UrlTemplate =
            "https://api.coingecko.com/api/v3/simple/price?ids=the-open-network&vs_currencies={0}";

        /// <inheritdoc />
        public async Task<PriceResult> GetAsync(
            string symbol,
            string vsCurrency = "usd",
            CancellationToken ct = default)
        {
            try
            {
                var url = string.Format(UrlTemplate, vsCurrency);
                var resp = await _httpClient.GetFromJsonAsync<Response>(url);
                var price = resp?.Ton?.Usd ?? 0m;
                return new PriceResult(symbol, vsCurrency, price, DateTimeOffset.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch price from CoinGecko");
                return new PriceResult(symbol, vsCurrency, 0m, DateTimeOffset.UtcNow);
            }
        }

        private sealed class Response
        {
            [JsonPropertyName("the-open-network")]
            public TonPrice? Ton { get; set; }
        }

        private sealed class TonPrice
        {
            [JsonPropertyName("usd")]
            public decimal Usd { get; set; }
        }
    }
}
