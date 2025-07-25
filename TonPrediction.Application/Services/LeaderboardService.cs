using QYQ.Base.Common.ApiResult;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Enums;
using TonPrediction.Application.Extensions;
using TonPrediction.Application.Output;
using TonPrediction.Application.Services.Interface;
using System.Linq;

namespace TonPrediction.Application.Services;

/// <summary>
/// 排行榜业务实现。
/// </summary>
public class LeaderboardService(IPnlStatRepository repo) : ILeaderboardService
{
    private readonly IPnlStatRepository _repo = repo;

    /// <inheritdoc />
    public async Task<ApiResult<LeaderboardOutput>> GetListAsync(
        string symbol = "ton",
        RankByType rankBy = RankByType.NetProfit,
        int page = 1,
        int pageSize = 10,
        string? address = null)
    {
        var api = new ApiResult<LeaderboardOutput>();
        page = page <= 0 ? 1 : page;
        pageSize = pageSize is <= 0 or > 100 ? 10 : pageSize;
        var stats = await _repo.GetPagedAsync(symbol, rankBy, page, pageSize);
        var list = new List<LeaderboardItemOutput>();
        for (var i = 0; i < stats.Count; i++)
        {
            var s = stats[i];
            list.Add(new LeaderboardItemOutput
            {
                Rank = (page - 1) * pageSize + i + 1,
                Address = s.UserAddress.ToFriendlyAddress(),
                Rounds = s.Rounds,
                WinRounds = s.WinRounds,
                LoseRounds = s.Rounds - s.WinRounds,
                WinRate = s.Rounds > 0 ? ((decimal)s.WinRounds / s.Rounds).ToString("F2") : "0",
                TotalBet = s.TotalBet.ToAmountString(),
                TotalReward = s.TotalReward.ToAmountString(),
                NetProfit = (s.TotalReward - s.TotalBet).ToAmountString()
            });
        }

        var output = new LeaderboardOutput { List = list };
        if (!string.IsNullOrWhiteSpace(address))
        {
            var rank = await _repo.GetRankAsync(symbol, address.ToRawAddress(), rankBy);
            if (rank > 0)
            {
                var stat = await _repo.GetByAddressAsync(symbol, address.ToRawAddress());
                if (stat != null)
                {
                    output.Self = new LeaderboardItemOutput
                    {
                        Rank = rank,
                        Address = stat.UserAddress.ToFriendlyAddress(),
                        Rounds = stat.Rounds,
                        WinRounds = stat.WinRounds,
                        LoseRounds = stat.Rounds - stat.WinRounds,
                        WinRate = stat.Rounds > 0 ? ((decimal)stat.WinRounds / stat.Rounds).ToString("F2") : "0",
                        TotalBet = stat.TotalBet.ToAmountString(),
                        TotalReward = stat.TotalReward.ToAmountString(),
                        NetProfit = (stat.TotalReward - stat.TotalBet).ToAmountString()
                    };
                }
            }
        }

        api.SetRsult(ApiResultCode.Success, output);
        return api;
    }

    /// <inheritdoc />
    public async Task<ApiResult<LeaderboardItemOutput?>> GetByAddressAsync(
        string address,
        string symbol = "ton",
        RankByType rankBy = RankByType.NetProfit)
    {
        var api = new ApiResult<LeaderboardItemOutput?>();
        if (string.IsNullOrWhiteSpace(address))
        {
            api.SetRsult(ApiResultCode.ErrorParams, null);
            return api;
        }

        var rank = await _repo.GetRankAsync(symbol, address.ToRawAddress(), rankBy);
        if (rank <= 0)
        {
            api.SetRsult(ApiResultCode.DataNotFound, null);
            return api;
        }

        var stat = await _repo.GetByAddressAsync(symbol, address.ToRawAddress());
        if (stat == null)
        {
            api.SetRsult(ApiResultCode.DataNotFound, null);
            return api;
        }

        var output = new LeaderboardItemOutput
        {
            Rank = rank,
            Address = stat.UserAddress.ToFriendlyAddress(),
            Rounds = stat.Rounds,
            WinRounds = stat.WinRounds,
            LoseRounds = stat.Rounds - stat.WinRounds,
            WinRate = stat.Rounds > 0 ? ((decimal)stat.WinRounds / stat.Rounds).ToString("F2") : "0",
            TotalBet = stat.TotalBet.ToAmountString(),
            TotalReward = stat.TotalReward.ToAmountString(),
            NetProfit = (stat.TotalReward - stat.TotalBet).ToAmountString()
        };

        api.SetRsult(ApiResultCode.Success, output);
        return api;
    }

    /// <inheritdoc />
    public async Task<ApiResult<AddressListOutput>> SearchAddressAsync(
        string keyword,
        string symbol = "ton",
        int limit = 10)
    {
        var api = new ApiResult<AddressListOutput>();
        if (string.IsNullOrWhiteSpace(keyword))
        {
            api.SetRsult(ApiResultCode.ErrorParams, new AddressListOutput());
            return api;
        }

        limit = limit is <= 0 or > 100 ? 10 : limit;
        var list = await _repo.SearchAddressAsync(symbol, keyword.ToRawAddress(), limit);
        var output = new AddressListOutput { Addresses = list.Select(a => a.ToFriendlyAddress()).ToList() };
        api.SetRsult(ApiResultCode.Success, output);
        return api;
    }
}
