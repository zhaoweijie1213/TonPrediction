namespace TonPrediction.Application.Output;

/// <summary>
/// 排行榜结果。
/// </summary>
public class LeaderboardOutput
{
    /// <summary>
    /// 排行榜列表。
    /// </summary>
    public List<LeaderboardItemOutput> List { get; set; } = new();

    /// <summary>
    /// 指定地址的排名，可为空。
    /// </summary>
    public int? AddressRank { get; set; }

    /// <summary>
    /// 指定地址所在页码，可为空。
    /// </summary>
    public int? AddressPage { get; set; }
}
