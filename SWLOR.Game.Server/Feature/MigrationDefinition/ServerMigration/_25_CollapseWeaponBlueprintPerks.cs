using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _25_CollapseWeaponBlueprintPerks : ServerMigrationBase, IServerMigration
    {
        public int Version => 25;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;

        public void Migrate()
        {
            var query = new DBQuery<Player>();
            var count = (int)DB.SearchCount(query);
            var players = DB.SearchRawJson(query.AddPaging(count, 0));
            var migratedCount = 0;
            var refundTotal = 0;

            foreach (var rawPlayer in players)
            {
                var jObject = JObject.Parse(rawPlayer);
                if (!WeaponBlueprintPerkMigration.CollapsePlayerPerks(jObject, out var refundAmount))
                    continue;

                var player = jObject.ToObject<Player>();
                if (refundAmount > 0)
                {
                    player.UnallocatedSP += refundAmount;
                    refundTotal += refundAmount;
                }

                DB.Set(player);
                migratedCount++;
            }

            Log.Write(LogGroup.Migration, $"Collapsed weapon blueprint perks for {migratedCount} players. Refunded {refundTotal} SP from duplicate legacy weapon blueprint perks.");
        }
    }
}
