using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TonPrediction.Application.Cache
{
    /// <summary>
    /// 缓存key集合
    /// </summary>
    public static class CacheKeyCollection
    {
        /// <summary>
        /// 生成回合缓存key。
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static string GetTonPriceKey(string address)
        {
            return $"{AppDomain.CurrentDomain.FriendlyName}tonprice:{address}";
        }
    }
}
