using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TonPrediction.Infrastructure.Services;
using Xunit;

namespace TonPrediction.Test;

/// <summary>
/// TonWalletService 单元测试。
/// </summary>
public class TonWalletServiceTests
{
    private sealed class FakeHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"hash\":\"h\",\"lt\":1}")
            };
            return Task.FromResult(resp);
        }
    }

    private sealed class FakeFactory(HttpClient client) : IHttpClientFactory
    {
        private readonly HttpClient _client = client;
        public HttpClient CreateClient(string name) => _client;
    }

    [Fact]
    public async Task TransferAsync_CallsTonApi()
    {
        var handler = new FakeHandler();
        var client = new HttpClient(handler) { BaseAddress = new System.Uri("https://tonapi.io") };
        var factory = new FakeFactory(client);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
            new KeyValuePair<string,string>("ENV_MASTER_WALLET_ADDRESS","master")
        }).Build();
        var service = new TonWalletService(config, factory, NullLogger<TonWalletService>.Instance);

        var result = await service.TransferAsync("addr", 1m);

        Assert.Equal("h", result.TxHash);
        Assert.NotNull(handler.Request);
        Assert.Equal("/v2/blockchain/accounts/master/transfer", handler.Request!.RequestUri!.AbsolutePath);
        var body = JsonDocument.Parse(await handler.Request.Content!.ReadAsStringAsync());
        Assert.Equal("addr", body.RootElement.GetProperty("to").GetString());
    }
}
