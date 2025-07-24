using DotNetCore.CAP;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QYQ.Base.Common.IOCExtensions;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Events;
using TonPrediction.Application.Services.Interface;

namespace TonPrediction.Api.Services;

/// <summary>
/// 处理平局回合退款的 CAP 事件订阅者。
/// </summary>
public class RefundEventHandler(IServiceScopeFactory scopeFactory, ILogger<RefundEventHandler> logger) : ICapSubscribe, ITransientDependency
{
    /// <summary>
    /// 订阅退款事件并执行转账。
    /// </summary>
    /// <param name="evt">包含回合信息的事件。</param>
    [CapSubscribe("round.refund.tie")]
    public async Task HandleAsync(RoundRefundEvent evt)
    {
        logger.LogInformation("HandleAsync.开始处理平局退款:{RoundId}", evt.RoundId);
        using var scope = scopeFactory.CreateScope();
        var betRepo = scope.ServiceProvider.GetRequiredService<IBetRepository>();
        var txRepo = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
        var roundRepo = scope.ServiceProvider.GetRequiredService<IRoundRepository>();
        var wallet = scope.ServiceProvider.GetRequiredService<IWalletService>();

        var round = await roundRepo.GetByIdAsync(evt.RoundId);
        if (round == null)
        {
            logger.LogWarning("HandleAsync.未找到回合:{RoundId}", evt.RoundId);
            return;
        }

        var bets = await betRepo.GetByRoundAsync(evt.RoundId);
        foreach (var bet in bets)
        {
            if (bet.Claimed) continue;
            var result = await wallet.TransferAsync(bet.UserAddress, bet.Amount, $"Refund {round.Epoch} {round.Symbol}");
            await txRepo.InsertAsync(new TransactionEntity
            {
                BetId = bet.Id,
                UserAddress = bet.UserAddress,
                Amount = bet.Amount,
                TxHash = result.TxHash,
                Status = result.Status,
                Lt = result.Lt,
                Timestamp = result.Timestamp
            });
            bet.Claimed = true;
            await betRepo.UpdateByPrimaryKeyAsync(bet);
        }
    }
}
