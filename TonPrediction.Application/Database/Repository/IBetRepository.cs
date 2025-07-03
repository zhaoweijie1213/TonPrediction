using TonPrediction.Application.Database.Entities;
using QYQ.Base.Common.IOCExtensions;
using QYQ.Base.SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TonPrediction.Application.Database.Repository
{
    /// <summary>
    /// 下注仓库接口
    /// </summary>
    public interface IBetRepository : IBaseRepository<BetEntity>, ITransientDependency
    {

    }
}
