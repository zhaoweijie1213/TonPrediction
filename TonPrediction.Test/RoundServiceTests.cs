using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Moq;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Enums;
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
                TotalAmount = 100m,
                BullAmount = 60m,
                BearAmount = 40m,
                RewardAmount = 95m
            },
            new()
            {
                Id = 2,
                Epoch = 2,
                StartTime = DateTime.UtcNow.AddMinutes(-5),
                CloseTime = DateTime.UtcNow,
                LockPrice = 1.2m,
                ClosePrice = 1.1m,
                TotalAmount = 80m,
                BullAmount = 30m,
                BearAmount = 50m,
                RewardAmount = 76m
            }
        };

        var bets = new List<BetEntity>
        {
            new()
            {
                RoundId = 1,
                UserAddress = "addr",
                Amount = 10m,
                Position = Position.Bull,
                Claimed = false,
                Reward = 20m
            }
        };

        var roundRepo = new Mock<IRoundRepository>();
        roundRepo.Setup(r => r.GetRecentAsync("ton", 2)).ReturnsAsync(rounds);
        var betRepo = new Mock<IBetRepository>();
        betRepo.Setup(b => b.GetByAddressAndRoundsAsync("addr", It.IsAny<long[]>(), default))
            .ReturnsAsync(bets);
        var service = new RoundService(roundRepo.Object, new ConfigurationBuilder().Build(), betRepo.Object);

        var result = await service.GetRecentAsync("addr", "ton", 2);

        Assert.Equal(2, result.Data.Count);
        Assert.Equal("10", result.Data[0].BetAmount);
    }
}
