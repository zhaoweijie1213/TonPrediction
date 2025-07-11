using DotNetCore.CAP;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Events;

namespace TonPrediction.Api.Services;

/// <summary>
/// 处理回合结算后统计数据的 CAP 事件订阅者。
/// </summary>
public class StatEventHandler(IServiceScopeFactory scopeFactory, ILogger<StatEventHandler> logger)
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<StatEventHandler> _logger = logger;

    /// <summary>
    /// 订阅回合统计事件并更新用户盈亏数据。
    /// </summary>
    /// <param name="evt">包含回合信息的事件。</param>
    [CapSubscribe("round.stat.update")]
    public async Task HandleAsync(RoundStatEvent evt)
    {
        using var scope = _scopeFactory.CreateScope();
        var betRepo = scope.ServiceProvider.GetRequiredService<IBetRepository>();
        var statRepo = scope.ServiceProvider.GetRequiredService<IPnlStatRepository>();
        var bets = await betRepo.GetByRoundAsync(evt.RoundId);
        foreach (var bet in bets)
        {
            var reward = bet.Reward;
            var stat = await statRepo.GetByAddressAsync(evt.Symbol, bet.UserAddress);
            var profit = reward - bet.Amount;
            var win = reward > 0;
            if (stat == null)
            {
                stat = new PnlStatEntity
                {
                    Symbol = evt.Symbol,
                    UserAddress = bet.UserAddress,
                    TotalBet = bet.Amount,
                    TotalReward = reward,
                    Rounds = 1,
                    WinRounds = win ? 1 : 0,
                    BestRoundId = evt.RoundId,
                    BestRoundProfit = profit
                };
                await statRepo.InsertAsync(stat);
            }
            else
            {
                stat.TotalBet += bet.Amount;
                stat.TotalReward += reward;
                stat.Rounds += 1;
                if (win) stat.WinRounds += 1;
                if (profit > stat.BestRoundProfit)
                {
                    stat.BestRoundProfit = profit;
                    stat.BestRoundId = evt.RoundId;
                }
                await statRepo.UpdateByPrimaryKeyAsync(stat);
            }
        }
    }
}
