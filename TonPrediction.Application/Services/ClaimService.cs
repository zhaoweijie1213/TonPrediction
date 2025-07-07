using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Input;
using TonPrediction.Application.Output;
using TonPrediction.Application.Services.Interface;

namespace TonPrediction.Application.Services;

/// <summary>
/// 领奖业务实现。
/// </summary>
public class ClaimService(
    IBetRepository betRepo,
    IClaimRepository claimRepo,
    IWalletService walletService) : IClaimService
{
    private readonly IBetRepository _betRepo = betRepo;
    private readonly IClaimRepository _claimRepo = claimRepo;
    private readonly IWalletService _walletService = walletService;

    /// <inheritdoc />
    public async Task<ClaimOutput?> ClaimAsync(ClaimInput input, CancellationToken ct = default)
    {
        var bet = await _betRepo.GetByAddressAndRoundAsync(input.Address, input.RoundId, ct);
        if (bet == null || bet.Claimed || bet.Reward <= 0m)
            return null;

        var result = await _walletService.TransferAsync(input.Address, bet.Reward, ct);

        var entity = new ClaimEntity
        {
            RoundId = input.RoundId,
            UserAddress = input.Address,
            Reward = bet.Reward,
            TxHash = result.TxHash,
            Status = result.Status,
            Lt = result.Lt,
            Timestamp = result.Timestamp
        };
        await _claimRepo.InsertAsync(entity);

        bet.Claimed = true;
        await _betRepo.UpdateByPrimaryKeyAsync(bet);

        return new ClaimOutput
        {
            TxHash = result.TxHash,
            Lt = result.Lt,
            Status = result.Status,
            Timestamp = new DateTimeOffset(result.Timestamp).ToUnixTimeSeconds()
        };
    }
}
