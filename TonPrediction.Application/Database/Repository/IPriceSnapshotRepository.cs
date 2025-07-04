using TonPrediction.Application.Database.Entities;
using QYQ.Base.Common.IOCExtensions;
using QYQ.Base.SqlSugar;

namespace TonPrediction.Application.Database.Repository
{
    /// <summary>
    /// 价格快照仓库接口。
    /// </summary>
    public interface IPriceSnapshotRepository : IBaseRepository<PriceSnapshotEntity>, ITransientDependency
    {
    }
}
