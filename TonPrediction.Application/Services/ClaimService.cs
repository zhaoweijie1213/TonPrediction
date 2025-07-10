using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Input;
using TonPrediction.Application.Output;
using Microsoft.Extensions.Configuration;
using TonPrediction.Application.Services.Interface;
using QYQ.Base.Common.ApiResult;

namespace TonPrediction.Application.Services;

/// <summary>
/// 领奖业务实现。
/// </summary>
public class ClaimService(
    IBetRepository betRepo,
    IClaimRepository claimRepo,
    IWalletService walletService,
    IConfiguration configuration) : IClaimService
{
    private readonly IBetRepository _betRepo = betRepo;
    private readonly IClaimRepository _claimRepo = claimRepo;
    private readonly IWalletService _walletService = walletService;
    private readonly IConfiguration _configuration = configuration;

    /// <inheritdoc />
    public async Task<ApiResult<ClaimOutput?>> ClaimAsync(ClaimInput input)
    {
        var api = new ApiResult<ClaimOutput?>();
        var bet = await _betRepo.GetByAddressAndRoundAsync(input.Address, input.RoundId);
        if (bet == null || bet.Claimed || bet.Reward <= 0m)
        {
            api.SetRsult(ApiResultCode.DataNotFound, null);
            return api;
        }

        var rate = _configuration.GetValue<decimal>("TreasuryFeeRate", 0.03m);
        var fee = bet.Reward * rate;
        var amount = bet.Reward - fee;
        var result = await _walletService.TransferAsync(input.Address, amount);

        var entity = new ClaimEntity
        {
            RoundId = input.RoundId,
            UserAddress = input.Address,
            Reward = amount,
            TxHash = result.TxHash,
            Status = result.Status,
            Lt = result.Lt,
            Timestamp = result.Timestamp
        };
        await _claimRepo.InsertAsync(entity);

        bet.Claimed = true;
        bet.TreasuryFee = fee;
        await _betRepo.UpdateByPrimaryKeyAsync(bet);

        var output = new ClaimOutput
        {
            TxHash = result.TxHash,
            Lt = result.Lt,
            Status = result.Status,
            Timestamp = new DateTimeOffset(result.Timestamp).ToUnixTimeSeconds()
        };

        api.SetRsult(ApiResultCode.Success, output);
        return api;
    }
}
