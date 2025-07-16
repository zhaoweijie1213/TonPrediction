using TonPrediction.Application.Database.Entities;
using QYQ.Base.Common.IOCExtensions;
using QYQ.Base.SqlSugar;

namespace TonPrediction.Application.Database.Repository
{
    /// <summary>
    /// 领奖与退款交易记录仓库接口。
    /// </summary>
    public interface ITransactionRepository : IBaseRepository<TransactionEntity>, ITransientDependency
    {
    }
}
