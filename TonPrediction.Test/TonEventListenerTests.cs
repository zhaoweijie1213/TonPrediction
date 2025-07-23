using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TonPrediction.Api.Services;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Enums;
using TonPrediction.Application.Config;
using TonPrediction.Application.Services.Interface;
using TonPrediction.Application.Services;
using Microsoft.Extensions.Options;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TonPrediction.Test;

/// <summary>
/// TonEventListener 单元测试。
/// </summary>
public class TonEventListenerTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{

    [Fact]
    public async Task ProcessTransactionAsync_InsertsBetAndUpdatesRound()
    {
        var lockTime = DateTime.UtcNow;
        var round = new RoundEntity
        {
            Symbol = "ton",
            Id = 1,
            Epoch = 1,
            CloseTime = DateTime.UtcNow.AddMinutes(5),
            LockTime = lockTime,
            LockPrice = 1m,
            Status = RoundStatus.Locked
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
        notifier.Setup(n => n.PushBetPlacedAsync(
                "sender",
                round.Id,
                round.Epoch,
                2,
                "hash"))
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

        var walletConfig = new WalletConfig { MasterWalletAddress = "addr" };

        var option = Mock.Of<IOptionsMonitor<PredictionConfig>>(o => o.CurrentValue == new PredictionConfig());
        var listener = factory.Services.GetRequiredService<TonEventListener>();

        var tx = new TonTxDetail
        {
            Hash = "hash",
            Lt = 1,
            Utime = (ulong)new DateTimeOffset(lockTime).ToUnixTimeSeconds(),
            In_Msg = new InMsg
            {
                Source = new AddressInfo { Address = "sender" },
                Destination = new AddressInfo { Address = "addr" },
                Value = 2,
                Decoded_Body = new DecodedBody { Text = "Bet 1 bull" }
            }
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
        var lockTime2 = DateTime.UtcNow;
        var round = new RoundEntity
        {
            Symbol = "ton",
            Id = 1,
            Epoch = 1,
            CloseTime = DateTime.UtcNow.AddMinutes(5),
            LockTime = lockTime2,
            LockPrice = 1m,
            Status = RoundStatus.Locked
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
        notifier.Setup(n => n.PushBetPlacedAsync(
                "sender",
                round.Id,
                round.Epoch,
                1,
                "hash"))
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

        var walletConfig = new WalletConfig { MasterWalletAddress = "addr" };

        var option = Mock.Of<IOptionsMonitor<PredictionConfig>>(o => o.CurrentValue == new PredictionConfig());
        var listener = factory.Services.GetRequiredService<TonEventListener>();

        var tx = new TonTxDetail
        {
            Hash = "hash",
            Lt = 2,
            Utime = (ulong)new DateTimeOffset(lockTime2).ToUnixTimeSeconds(),
            In_Msg = new InMsg
            {
                Source = new AddressInfo { Address = "sender" },
                Destination = new AddressInfo { Address = "addr" },
                Value = 1,
                Decoded_Body = new DecodedBody { Text = "Bet 1 bull" }
            }
        };
        await listener.ProcessTransactionAsync(tx);

        betRepo.Verify(b => b.UpdateByPrimaryKeyAsync(bet), Times.Once);
    }
}
