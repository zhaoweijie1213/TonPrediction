using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using TonPrediction.Application.Services;

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
        private const string Url = "https://api.coingecko.com/api/v3/simple/price?ids=the-open-network&vs_currencies=usd";

        /// <inheritdoc />
        public async Task<decimal> GetCurrentPriceAsync(CancellationToken token)
        {
            try
            {
                var resp = await _httpClient.GetFromJsonAsync<Response>(Url, token);
                return resp?.Ton?.Usd ?? 0m;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch price from CoinGecko");
                return 0m;
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
