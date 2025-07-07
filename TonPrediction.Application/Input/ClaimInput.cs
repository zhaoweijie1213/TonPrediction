namespace TonPrediction.Application.Input;

/// <summary>
/// 领奖请求参数。
/// </summary>
public class ClaimInput
{
    /// <summary>
    /// 回合编号。
    /// </summary>
    public long RoundId { get; set; }

    /// <summary>
    /// 用户地址。
    /// </summary>
    public string Address { get; set; } = string.Empty;
}
