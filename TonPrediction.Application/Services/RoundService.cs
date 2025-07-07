using Microsoft.Extensions.Configuration;
using SqlSugar;
using TonPrediction.Application.Database.Entities;
using TonPrediction.Application.Database.Repository;
using TonPrediction.Application.Enums;
using TonPrediction.Application.Output;
using TonPrediction.Application.Services.Interface;

namespace TonPrediction.Application.Services;

/// <summary>
/// 回合信息查询业务实现。
/// </summary>
public class RoundService(
    IRoundRepository roundRepo,
    IConfiguration configuration) : IRoundService
{
    private readonly IRoundRepository _roundRepo = roundRepo;
    private readonly IConfiguration _configuration = configuration;

    /// <inheritdoc />
    public async Task<List<RoundHistoryOutput>> GetHistoryAsync(
        string symbol = "ton",
        int limit = 3,
        CancellationToken ct = default)
    {
        limit = limit is <= 0 or > 100 ? 3 : limit;
        var list = await _roundRepo.GetEndedAsync(symbol, limit, ct);
        return list.Select(r => new RoundHistoryOutput
        {
            RoundId = r.Id,
            LockPrice = r.LockPrice.ToString("F8"),
            ClosePrice = r.ClosePrice.ToString("F8"),
            TotalAmount = r.TotalAmount.ToString("F8"),
            UpAmount = r.BullAmount.ToString("F8"),
            DownAmount = r.BearAmount.ToString("F8"),
            RewardPool = r.RewardAmount.ToString("F8"),
            EndTime = new DateTimeOffset(r.CloseTime).ToUnixTimeSeconds(),
            OddsUp = r.BullAmount > 0m ? (r.TotalAmount / r.BullAmount).ToString("F8") : "0",
            OddsDown = r.BearAmount > 0m ? (r.TotalAmount / r.BearAmount).ToString("F8") : "0"
        }).ToList();
    }

    /// <summary>
    /// 获取即将开始的回合时间列表。
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<List<UpcomingRoundOutput>> GetUpcomingAsync(
        string symbol = "ton",
        CancellationToken ct = default)
    {
        var latest = await _roundRepo.GetLatestAsync(symbol, ct);
        var intervalSec = _configuration.GetValue<int>("ENV_ROUND_INTERVAL_SEC", 300);
        var startTime = latest?.CloseTime ?? DateTime.UtcNow;
        var list = new List<UpcomingRoundOutput>();
        for (var i = 0; i < 2; i++)
        {
            var s = startTime.AddSeconds(intervalSec * i);
            list.Add(new UpcomingRoundOutput
            {
                RoundId = new DateTimeOffset(s).ToUnixTimeSeconds(),
                StartTime = new DateTimeOffset(s).ToUnixTimeSeconds(),
                EndTime = new DateTimeOffset(s.AddSeconds(intervalSec)).ToUnixTimeSeconds()
            });
        }
        return list;
    }
}
