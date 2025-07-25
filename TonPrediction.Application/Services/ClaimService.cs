using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Input;
using TonPrediction.Application.Output;
using Microsoft.Extensions.Configuration;
using TonPrediction.Application.Services.Interface;
using TonPrediction.Application.Extensions;
using QYQ.Base.Common.ApiResult;
using TonPrediction.Application.Enums;

namespace TonPrediction.Application.Services;

/// <summary>
/// 领奖业务实现。
/// </summary>
public class ClaimService(
    IBetRepository betRepo,
    ITransactionRepository txRepo,
    IWalletService walletService,
    IConfiguration configuration) : IClaimService
{
    private readonly IBetRepository _betRepo = betRepo;
    private readonly ITransactionRepository _txRepo = txRepo;
    private readonly IWalletService _walletService = walletService;
    private readonly IConfiguration _configuration = configuration;

    /// <inheritdoc />
    public async Task<ApiResult<ClaimOutput?>> ClaimAsync(ClaimInput input)
    {
        var api = new ApiResult<ClaimOutput?>();
        var rawAddress = input.Address.ToRawAddress();
        var bet = await _betRepo.GetByAddressAndRoundAsync(rawAddress, input.RoundId);
        if (bet == null || bet.Claimed || bet.Reward <= 0)
        {
            api.SetRsult(ApiResultCode.DataNotFound, null);
            return api;
        }
        var amount = bet.Reward;

        var result = await _walletService.TransferAsync(input.Address, amount);

        if (result.Status == ClaimStatus.Confirmed)
        {
            var entity = new TransactionEntity
            {
                BetId = bet.Id,
                UserAddress = rawAddress,
                Amount = amount,
                TxHash = result.TxHash,
                Status = result.Status,
                Lt = result.Lt,
                Timestamp = result.Timestamp
            };
            await _txRepo.InsertAsync(entity);

            bet.Claimed = true;
            await _betRepo.UpdateByPrimaryKeyAsync(bet);

            var output = new ClaimOutput
            {
                TxHash = result.TxHash,
                Lt = result.Lt,
                Status = result.Status,
                Timestamp = new DateTimeOffset(result.Timestamp).ToUnixTimeSeconds()
            };

            api.SetRsult(ApiResultCode.Success, output);
        }
        else
        {
            api.SetRsult(ApiResultCode.Fail, null);
        }
        return api;
    }
}
