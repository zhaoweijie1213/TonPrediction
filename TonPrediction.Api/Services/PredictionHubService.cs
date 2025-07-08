using Microsoft.AspNetCore.SignalR;
using PancakeSwap.Api.Hubs;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Output;
using TonPrediction.Application.Services.Interface;

namespace TonPrediction.Api.Services;

/// <summary>
/// SignalR 推送实现。
/// </summary>
public class PredictionHubService(IHubContext<PredictionHub> hub) : IPredictionHubService
{
    private readonly IHubContext<PredictionHub> _hub = hub;

    /// <inheritdoc />
    public Task PushCurrentRoundAsync(RoundEntity round, decimal currentPrice, CancellationToken ct = default)
    {
        var oddsBull = round.BullAmount > 0m ? round.TotalAmount / round.BullAmount : 0m;
        var oddsBear = round.BearAmount > 0m ? round.TotalAmount / round.BearAmount : 0m;
        var output = new CurrentRoundOutput
        {
            RoundId = round.Epoch,
            LockPrice = round.LockPrice.ToString("F8"),
            CurrentPrice = currentPrice.ToString("F8"),
            TotalAmount = round.TotalAmount.ToString("F8"),
            BullAmount = round.BullAmount.ToString("F8"),
            BearAmount = round.BearAmount.ToString("F8"),
            RewardPool = round.RewardAmount.ToString("F8"),
            EndTime = new DateTimeOffset(round.CloseTime).ToUnixTimeSeconds(),
            BullOdds = oddsBull.ToString("F8"),
            BearOdds = oddsBear.ToString("F8"),
            Status = round.Status
        };
        return _hub.Clients.All.SendAsync("currentRound", output, ct);
    }

    /// <inheritdoc />
    public Task PushRoundStartedAsync(long roundId, CancellationToken ct = default) =>
        _hub.Clients.All.SendAsync("roundStarted", new RoundStartedOutput { RoundId = roundId }, ct);

    /// <inheritdoc />
    public Task PushRoundLockedAsync(long roundId, CancellationToken ct = default) =>
        _hub.Clients.All.SendAsync("roundLocked", new RoundLockedOutput { RoundId = roundId }, ct);

    /// <inheritdoc />
    public Task PushSettlementStartedAsync(long roundId, CancellationToken ct = default) =>
        _hub.Clients.All.SendAsync("settlementStarted", new SettlementStartedOutput { RoundId = roundId }, ct);

    /// <inheritdoc />
    public Task PushRoundEndedAsync(long roundId, CancellationToken ct = default) =>
        _hub.Clients.All.SendAsync("roundEnded", new RoundEndedOutput { RoundId = roundId }, ct);

    /// <inheritdoc />
    public Task PushSettlementEndedAsync(long roundId, CancellationToken ct = default) =>
        _hub.Clients.All.SendAsync("settlementEnded", new SettlementEndedOutput { RoundId = roundId }, ct);
}
