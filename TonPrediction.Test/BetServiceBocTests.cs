using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Enums;
using TonPrediction.Application.Services;
using TonPrediction.Application.Services.Interface;
using TonSdk.Core;
using TonSdk.Core.Block;
using TonSdk.Core.Boc;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace TonPrediction.Test;

/// <summary>
/// BetService ReportAsync 测试。
/// </summary>
public class BetServiceBocTests
{
    private sealed class FakeHandler : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;

            var detail = new TonTxDetail
            {
                Hash = "hash",
                Lt = 1,
                In_Msg = new InMsg
                {
                    Source = new AddressInfo { Address = "sender" },
                    Destination = new AddressInfo { Address = "dest" },
                    Value = 1_000_000,
                    Decoded_Body = new DecodedBody { Text = "Bet 1 bull" }
                }
            };
            var json = JsonConvert.SerializeObject(detail);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });
        }
    }

    private sealed class FakeHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        private readonly HttpClient _client = client;
        public HttpClient CreateClient(string name) => _client;
    }
    [Fact]
    public async Task ReportAsync_InsertsBet()
    {
        // 构造下注 boc
        var body = new CellBuilder().StoreUInt(0, 32).StoreString("Bet 1 bull").Build();
        var dest = new Address(0, new byte[32]);
        var info = new IntMsgInfo(new IntMsgInfoOptions
        {
            Src = new Address(0, new byte[32]),
            Dest = dest,
            Value = new Coins(1_000_000, new CoinsOptions { IsNano = true })
        });
        var msg = new MessageX(new MessageXOptions { Info = info, Body = body });
        var boc = msg.Cell.ToString("base64url");

        BetEntity? inserted = null;
        var betRepo = new Mock<IBetRepository>();
        betRepo.Setup(b => b.GetByTxHashAsync(It.IsAny<string>())).ReturnsAsync((BetEntity?)null);
        betRepo.Setup(b => b.InsertAsync(It.IsAny<BetEntity>()))
            .Callback<BetEntity>(b => inserted = b)
            .ReturnsAsync(new BetEntity());
        var round = new RoundEntity { Id = 1, Status = RoundStatus.Betting };
        var roundRepo = new Mock<IRoundRepository>();
        roundRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(round);
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string,string>("ENV_MASTER_WALLET_ADDRESS", "dest")
            })
            .Build();
        var handler = new FakeHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tonapi.io") };
        var factory = new FakeHttpClientFactory(httpClient);
        var service = new BetService(
            factory,
            cfg,
            betRepo.Object,
            roundRepo.Object,
            Mock.Of<IPredictionHubService>());

        var result = await service.ReportAsync("sender", boc);

        Assert.Equal("hash", result.Data);
        Assert.NotNull(inserted);
        Assert.Equal(1, inserted!.RoundId);
        Assert.NotNull(handler.RequestUri);
        Assert.Contains("/v2/blockchain/accounts/", handler.RequestUri!.AbsolutePath);
    }
}
