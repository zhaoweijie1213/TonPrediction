using TonPrediction.Application.Database.Entities;
using QYQ.Base.Common.IOCExtensions;
using QYQ.Base.SqlSugar;

namespace TonPrediction.Application.Database.Repository
{
    /// <summary>
    /// 领奖记录仓库接口。
    /// </summary>
    public interface IClaimRepository : IBaseRepository<ClaimEntity>, ITransientDependency
    {
    }
}
