using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Enums;
using TonPrediction.Application.Config;
using TonPrediction.Application.Services;
using Xunit;

namespace TonPrediction.Test;

/// <summary>
/// RoundService 最近回合接口测试。
/// </summary>
public class RoundServiceTests
{
    [Fact]
    public async Task GetRecentAsync_ReturnsRoundsWithBetInfo()
    {
        var rounds = new List<RoundEntity>
        {
            new()
            {
                Id = 1,
                Epoch = 1,
                StartTime = DateTime.UtcNow.AddMinutes(-10),
                CloseTime = DateTime.UtcNow.AddMinutes(-5),
                LockPrice = 1m,
                ClosePrice = 1.2m,
                TotalAmount = 100000000000,
                BullAmount = 60000000000,
                BearAmount = 40000000000,
                RewardAmount = 95000000000,
                Status = RoundStatus.Completed,
                WinnerSide = Position.Bull
            },
            new()
            {
                Id = 2,
                Epoch = 2,
                StartTime = DateTime.UtcNow.AddMinutes(-5),
                CloseTime = DateTime.UtcNow,
                LockPrice = 1.2m,
                ClosePrice = 1.1m,
                TotalAmount = 80000000000,
                BullAmount = 30000000000,
                BearAmount = 50000000000,
                RewardAmount = 76000000000,
                Status = RoundStatus.Locked
            }
        };

        var bets = new List<BetEntity>
        {
            new()
            {
                RoundId = 1,
                UserAddress = "addr",
                Amount = 10000000000,
                Position = Position.Bull,
                Claimed = false,
                Reward = 20000000000
            }
        };

        var roundRepo = new Mock<IRoundRepository>();
        roundRepo.Setup(r => r.GetRecentAsync("ton", 2)).ReturnsAsync(rounds);
        var betRepo = new Mock<IBetRepository>();
        betRepo.Setup(b => b.GetByAddressAndRoundsAsync("addr", It.IsAny<long[]>(), default))
            .ReturnsAsync(bets);
        var option = Mock.Of<IOptionsMonitor<PredictionConfig>>(o => o.CurrentValue == new PredictionConfig());
        var service = new RoundService(roundRepo.Object, betRepo.Object, option);

        var result = await service.GetRecentAsync("addr", "ton", 2);

        Assert.Equal(2, result.Data.Count);
        Assert.Equal("10", result.Data[0].BetAmount);
        Assert.Equal(RoundStatus.Completed, result.Data[0].Status);
        Assert.Equal(Position.Bull, result.Data[0].WinnerSide);
        Assert.Equal(RoundStatus.Locked, result.Data[1].Status);
    }

    [Fact]
    public async Task GetRecentAsync_WithoutAddress_ReturnsRoundsOnly()
    {
        var rounds = new List<RoundEntity>
        {
            new()
            {
                Id = 1,
                Epoch = 1,
                StartTime = DateTime.UtcNow.AddMinutes(-10),
                CloseTime = DateTime.UtcNow.AddMinutes(-5),
                LockPrice = 1m,
                ClosePrice = 1.2m,
                TotalAmount = 100000000000,
                BullAmount = 60000000000,
                BearAmount = 40000000000,
                RewardAmount = 95000000000,
                Status = RoundStatus.Completed
            }
        };

        var roundRepo = new Mock<IRoundRepository>();
        roundRepo.Setup(r => r.GetRecentAsync("ton", 1)).ReturnsAsync(rounds);
        var betRepo = new Mock<IBetRepository>();
        var option = Mock.Of<IOptionsMonitor<PredictionConfig>>(o => o.CurrentValue == new PredictionConfig());
        var service = new RoundService(roundRepo.Object, betRepo.Object, option);

        var result = await service.GetRecentAsync(null, "ton", 1);

        Assert.Single(result.Data);
        Assert.Equal("0", result.Data[0].BetAmount);
        Assert.Equal(RoundStatus.Completed, result.Data[0].Status);
    }
}
