using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CurrencyService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Shared.Core.Log;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _9_CurrencyMigration : ServerMigrationBase, IServerMigration
    {
        public int Version => 9;

        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;

        public void Migrate()
        {
            var dbQuery = new DBQuery<Player>();
            var playerCount = (int)DB.SearchCount(dbQuery);

            var dbPlayers = DB.Search(dbQuery
                .AddPaging(playerCount, 0));

            foreach (var dbPlayer in dbPlayers)
            {
                var json = DB.GetRawJson<Player>(dbPlayer.Id);
                var jObject = JObject.Parse(json);

                var perkResets = jObject["NumberPerkResetsAvailable"]?.Value<int>() ?? 0;
                var rebuildCount = jObject["NumberRebuildsAvailable"]?.Value<int>() ?? 0;

                dbPlayer.Currencies[CurrencyType.PerkRefundToken] = perkResets;
                dbPlayer.Currencies[CurrencyType.RebuildToken] = rebuildCount;

                DB.Set(dbPlayer);

                Log.Write(LogGroup.Migration, $"Migrated {perkResets} perk resets and {rebuildCount} rebuild tokens for player {dbPlayer.Name} ({dbPlayer.Id})");
            }
        }
    }
}
