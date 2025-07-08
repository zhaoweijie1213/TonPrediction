using QYQ.Base.Common.IOCExtensions;
using System.Threading;
using System.Threading.Tasks;

namespace TonPrediction.Application.Services.Interface;

/// <summary>
/// 下注上报业务接口。
/// </summary>
public interface IBetService : ITransientDependency
{
    /// <summary>
    /// 根据交易哈希上报下注。
    /// </summary>
    /// <param name="txHash">交易哈希。</param>
    /// <returns>是否受理成功。</returns>
    Task<bool> ReportAsync(string txHash);
}
