using EasyCaching.Redis;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TonPrediction.Infrastructure
{
    public static class ServiceExtension
    {
        /// <summary>
        /// 添加基础设施服务。
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static WebApplicationBuilder AddInfrastructure(this WebApplicationBuilder builder)
        {
            #region EasyCaching注册

            builder.Services.AddEasyCaching(options =>
            {
                //static void easycaching(EasyCachingJsonSerializerOptions x)
                //{
                //    x.NullValueHandling = NullValueHandling.Ignore;
                //    x.TypeNameHandling = TypeNameHandling.None;
                //}
                options.UseRedis(config =>
                {
                    config.DBConfig = builder.Configuration.GetSection("Redis").Get<RedisDBOptions>();
                }, "Redis").WithMessagePack("Redis");
            });

            #endregion
            return builder;
        }
    }
}
