using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _30_RemoveBeastSavingThrowPurities : ServerMigrationBase, IServerMigration
    {
        private const string SavingThrowPuritiesKey = "SavingThrowPurities";

        public int Version => 30;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;

        public void Migrate()
        {
            var beastCount = MigrateEntities<Beast>();
            var incubationJobCount = MigrateEntities<IncubationJob>();

            Log.Write(LogGroup.Migration, $"Removed legacy beast saving throw purities from {beastCount} beasts and {incubationJobCount} incubation jobs.");
        }

        private static int MigrateEntities<TEntity>()
            where TEntity : EntityBase
        {
            var query = new DBQuery<TEntity>();
            var count = (int)DB.SearchCount(query);
            var entities = DB.SearchRawJson(query.AddPaging(count, 0));
            var migratedCount = 0;

            foreach (var rawEntity in entities)
            {
                var jObject = JObject.Parse(rawEntity);
                if (!jObject.Remove(SavingThrowPuritiesKey))
                    continue;

                var entity = jObject.ToObject<TEntity>();
                DB.Set(entity);
                migratedCount++;
            }

            return migratedCount;
        }
    }
}
