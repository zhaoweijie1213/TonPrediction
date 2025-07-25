using System.Collections.Generic;

namespace TonPrediction.Application.Output;

/// <summary>
/// 地址列表结果。
/// </summary>
public class AddressListOutput
{
    /// <summary>
    /// 匹配到的地址集合。
    /// </summary>
    public List<string> Addresses { get; set; } = new();
}
