using Newtonsoft.Json.Linq;
using SWLOR.Core.Entity;
using SWLOR.Core.LogService;
using SWLOR.Core.Service;
using SWLOR.Core.Service.CurrencyService;
using SWLOR.Core.Service.DBService;
using SWLOR.Core.Service.MigrationService;

namespace SWLOR.Core.Feature.MigrationDefinition.ServerMigration
{
    public class _9_CurrencyMigration : ServerMigrationBase, IServerMigration
    {
        public int Version => 9;
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
