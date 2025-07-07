using SqlSugar;
using TonPrediction.Application.Database.Entities;

namespace TonPrediction.Infrastructure.Database.Migrations
{
    /// <summary>
    ///     Initial database migration.
    /// </summary>
    public static class InitMigration
    {
        /// <summary>
        ///     Executes the migration to create required tables.
        /// </summary>
        /// <param name="db">SqlSugar client.</param>
        public static void Run(ISqlSugarClient db)
        {
            db.CodeFirst.InitTables<RoundEntity>();
            db.CodeFirst.InitTables<BetEntity>();
            db.CodeFirst.InitTables<PriceSnapshotEntity>();
            db.CodeFirst.InitTables<ClaimEntity>();
            db.CodeFirst.InitTables<StateEntity>();
        }
    }
}
