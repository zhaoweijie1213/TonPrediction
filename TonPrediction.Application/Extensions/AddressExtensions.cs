using TonSdk.Core;

namespace TonPrediction.Application.Extensions;

/// <summary>
/// 钱包地址转换工具。
/// </summary>
public static class AddressExtensions
{
    /// <summary>
    /// 将地址转换为 Raw 格式，不合法时返回原字符串。
    /// </summary>
    /// <param name="address">待转换地址。</param>
    /// <returns>Raw 地址。</returns>
    public static string ToRawAddress(this string address)
    {
        if (string.IsNullOrWhiteSpace(address)) return address;
        try
        {
            var addr = new Address(address);
            return addr.ToString(AddressType.Raw, new AddressStringifyOptions(true, false, true, addr.GetWorkchain()));
        }
        catch
        {
            return address;
        }
    }

    /// <summary>
    /// 将地址转换为用户友好的 Base64 格式，不合法时返回原字符串。
    /// </summary>
    /// <param name="address">待转换地址。</param>
    /// <returns>Base64 地址。</returns>
    public static string ToFriendlyAddress(this string address)
    {
        if (string.IsNullOrWhiteSpace(address)) return address;
        try
        {
            var addr = new Address(address);
            return addr.ToString(AddressType.Base64, new AddressStringifyOptions(true, false, true, addr.GetWorkchain()));
        }
        catch
        {
            return address;
        }
    }
}
