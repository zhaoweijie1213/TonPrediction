using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TonPrediction.Api.Services;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Enums;
using TonPrediction.Application.Services.Interface;
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
            Epoch = 1,
            CloseTime = DateTime.UtcNow.AddMinutes(5),
            LockPrice = 1m,
            Status = RoundStatus.Betting
        };

        BetEntity? inserted = null;
        var betRepo = new Mock<IBetRepository>();
        betRepo.Setup(b => b.InsertAsync(It.IsAny<BetEntity>()))
            .Callback<BetEntity>(b => inserted = b)
            .ReturnsAsync(new BetEntity())
            .Verifiable();
        betRepo.Setup(b => b.GetByTxHashAsync("hash"))
            .ReturnsAsync((BetEntity?)null);

        var roundRepo = new Mock<IRoundRepository>();
        roundRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(round);
        roundRepo.Setup(r => r.UpdateByPrimaryKeyAsync(round))
            .ReturnsAsync(true)
            .Verifiable();

        var notifier = new Mock<IPredictionHubService>();
        notifier.Setup(n => n.PushNextRoundAsync(
                round,
                It.IsAny<decimal>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var stateRepo = new Mock<IStateRepository>();
        var sp = new ServiceCollection()
            .AddSingleton(betRepo.Object)
            .AddSingleton(roundRepo.Object)
            .AddSingleton(stateRepo.Object)
            .AddSingleton(notifier.Object)
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
            notifier.Object,
            NullLogger<TonEventListener>.Instance,
            new Mock<IHttpClientFactory>().Object,
            Mock.Of<IDistributedLock>());

        var tx = new TonTxDetail(2m, new InMsg("sender", "1 bull", "addr"), "hash")
        {
            Lt = 1
        };
        await listener.ProcessTransactionAsync(tx);

        betRepo.Verify(b => b.InsertAsync(It.IsAny<BetEntity>()), Times.Once);
        roundRepo.Verify(r => r.UpdateByPrimaryKeyAsync(round), Times.Once);
        notifier.Verify(n => n.PushNextRoundAsync(
            round,
            It.IsAny<decimal>()), Times.Once);

        Assert.Equal("hash", inserted?.TxHash);
        Assert.Equal<ulong>(1ul, inserted?.Lt ?? 0);
    }

    [Fact]
    public async Task ProcessTransactionAsync_UpdatesExistingBet()
    {
        var round = new RoundEntity
        {
            Symbol = "ton",
            Id = 1,
            Epoch = 1,
            CloseTime = DateTime.UtcNow.AddMinutes(5),
            LockPrice = 1m,
            Status = RoundStatus.Betting
        };

        var bet = new BetEntity { TxHash = "hash", Lt = 0, Status = BetStatus.Pending };
        var betRepo = new Mock<IBetRepository>();
        betRepo.Setup(b => b.GetByTxHashAsync("hash"))
            .ReturnsAsync(bet);
        betRepo.Setup(b => b.UpdateByPrimaryKeyAsync(bet)).ReturnsAsync(true).Verifiable();

        var roundRepo = new Mock<IRoundRepository>();
        roundRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(round);

        var notifier = new Mock<IPredictionHubService>();

        var stateRepo = new Mock<IStateRepository>();
        var sp = new ServiceCollection()
            .AddSingleton(betRepo.Object)
            .AddSingleton(roundRepo.Object)
            .AddSingleton(stateRepo.Object)
            .AddSingleton(notifier.Object)
            .BuildServiceProvider();
        var scope = new Mock<IServiceScope>();
        scope.SetupGet(s => s.ServiceProvider).Returns(sp);
        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> { { "ENV_MASTER_WALLET_ADDRESS", "addr" } })
            .Build();

        var listener = new TonEventListener(
            scopeFactory.Object,
            config,
            notifier.Object,
            NullLogger<TonEventListener>.Instance,
            new Mock<IHttpClientFactory>().Object,
            Mock.Of<IDistributedLock>());

        var tx = new TonTxDetail(1m, new InMsg("sender", "1 bull", "addr"), "hash") { Lt = 2 };
        await listener.ProcessTransactionAsync(tx);

        betRepo.Verify(b => b.UpdateByPrimaryKeyAsync(bet), Times.Once);
    }
}
