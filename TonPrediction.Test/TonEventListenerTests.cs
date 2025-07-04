using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PancakeSwap.Api.Hubs;
using TonPrediction.Api.Services;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Enums;
using Xunit;

namespace TonPrediction.Test;

/// <summary>
/// TonEventListener 单元测试。
/// </summary>
public class TonEventListenerTests
{
    [Fact]
    public async Task ProcessTransactionAsync_InsertsBetAndUpdatesRound()
    {
        var round = new RoundEntity
        {
            Symbol = "ton",
            Id = 1,
            CloseTime = DateTime.UtcNow.AddMinutes(5),
            LockPrice = 1m,
            Status = RoundStatus.Live
        };

        var betRepo = new Mock<IBetRepository>();
        betRepo.Setup(b => b.InsertAsync(It.IsAny<BetEntity>()))
            .ReturnsAsync(new BetEntity())
            .Verifiable();

        var roundRepo = new Mock<IRoundRepository>();
        roundRepo.Setup(r => r.GetCurrentLiveAsync("ton", It.IsAny<CancellationToken>()))
            .ReturnsAsync(round);
        roundRepo.Setup(r => r.UpdateByPrimaryKeyAsync(round))
            .ReturnsAsync(true)
            .Verifiable();

        var clientProxy = new Mock<IClientProxy>();
        clientProxy.Setup(p => p.SendCoreAsync(
                "currentRound",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();
        var hubClients = new Mock<IHubClients>();
        hubClients.SetupGet(h => h.All).Returns(clientProxy.Object);
        var hubContext = new Mock<IHubContext<PredictionHub>>();
        hubContext.SetupGet(h => h.Clients).Returns(hubClients.Object);

        var sp = new ServiceCollection()
            .AddSingleton(betRepo.Object)
            .AddSingleton(roundRepo.Object)
            .BuildServiceProvider();
        var scope = new Mock<IServiceScope>();
        scope.SetupGet(s => s.ServiceProvider).Returns(sp);
        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"ENV_MASTER_WALLET_ADDRESS", "addr"}
            }).Build();

        var listener = new TonEventListener(
            scopeFactory.Object,
            config,
            hubContext.Object,
            NullLogger<TonEventListener>.Instance,
            new Mock<IHttpClientFactory>().Object);

        var tx = new TonTxDetail(2m, new InMsg("sender", "ton bull"));
        await listener.ProcessTransactionAsync(tx, CancellationToken.None);

        betRepo.Verify(b => b.InsertAsync(It.IsAny<BetEntity>()), Times.Once);
        roundRepo.Verify(r => r.UpdateByPrimaryKeyAsync(round), Times.Once);
        clientProxy.Verify(p => p.SendCoreAsync(
            "currentRound",
            It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
