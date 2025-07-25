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
    /// 指定地址的排行信息，可为空。
    /// </summary>
    public LeaderboardItemOutput? Self { get; set; }
}
