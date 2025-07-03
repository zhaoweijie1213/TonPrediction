using Microsoft.Extensions.Configuration;
using SqlSugar;

namespace TonPrediction.Infrastructure.Database
{
    /// <summary>
    ///     SqlSugar database context.
    /// </summary>
    public class ApplicationDbContext(IConfiguration configuration)
    {
        /// <summary>
        ///     Gets the SqlSugar client instance.
        /// </summary>
        public ISqlSugarClient Db => GetDbContext();

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
        /// </summary>
        /// <param name="configuration">Configuration to fetch connection string.</param>
        public ISqlSugarClient GetDbContext()
        {
            var connectionString = configuration.GetConnectionString("Default");
            var db = new SqlSugarScope(new ConnectionConfig
            {
                ConnectionString = connectionString,
                DbType = DbType.MySql,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });

            return db;
        }
    }
}
