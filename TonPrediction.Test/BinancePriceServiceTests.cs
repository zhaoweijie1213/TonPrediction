using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using TonPrediction.Infrastructure.Services;
using Xunit;

namespace TonPrediction.Test;

/// <summary>
/// BinancePriceService 单元测试。
/// </summary>
public class BinancePriceServiceTests
{
    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"price\": 1}")
            };
            return Task.FromResult(resp);
        }
    }

    private sealed class FakeHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        private readonly HttpClient _client = client;
        public HttpClient CreateClient(string name) => _client;
    }

    /// <summary>
    /// GetAsync 应将 usd 转换为 usdt。
    /// </summary>
    [Fact]
    public async Task GetAsync_MapsUsdToUsdt()
    {
        var handler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var factory = new FakeHttpClientFactory(httpClient);
        var service = new BinancePriceService(factory, NullLogger<BinancePriceService>.Instance);

        var result = await service.GetAsync("ton", "usd");

        Assert.Equal(1m, result.Price);
        Assert.NotNull(handler.RequestUri);
        Assert.Contains("symbol=TONUSDT", handler.RequestUri!.Query);
    }
}
