using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Enums;
using TonPrediction.Application.Services;
using TonPrediction.Application.Services.Interface;
using System.Net.Http;
using Microsoft.Extensions.Options;
using TonPrediction.Application.Config;
using Xunit;

namespace TonPrediction.Test;

/// <summary>
/// BetService 验证接口测试。
/// </summary>
public class BetServiceTests
{
    [Fact]
    public async Task VerifyAsync_ReturnsSuccess_ForBettingRound()
    {
        var round = new RoundEntity
        {
            Id = 1,
            LockTime = DateTime.UtcNow.AddMinutes(1),
            Status = RoundStatus.Betting
        };
        var roundRepo = new Mock<IRoundRepository>();
        roundRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(round);
        var service = new BetService(
            Mock.Of<IHttpClientFactory>(),
            new ConfigurationBuilder().Build(),
            Mock.Of<IBetRepository>(),
            roundRepo.Object,
            Mock.Of<IPredictionHubService>(),
            Mock.Of<IOptionsMonitor<PredictionConfig>>());

        var result = await service.VerifyAsync(1, "user");

        Assert.True(result.Data);
    }
}
