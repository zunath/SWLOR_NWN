using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.PlayerMarketService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _27_RenameMarketWeaponCategories : ServerMigrationBase, IServerMigration
    {
        public int Version => 27;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;

        public void Migrate()
        {
            var query = new DBQuery<MarketItem>();
            var count = (int)DB.SearchCount(query);
            var items = DB.SearchRawJson(query.AddPaging(count, 0));
            var migratedCount = 0;

            foreach (var rawItem in items)
            {
                var jObject = JObject.Parse(rawItem);
                var categoryToken = jObject[nameof(MarketItem.Category)];

                if (!TryMapCategory(categoryToken, out var category))
                    continue;

                jObject[nameof(MarketItem.Category)] = (int)category;

                var item = jObject.ToObject<MarketItem>();
                DB.Set(item);
                migratedCount++;
            }

            Log.Write(LogGroup.Migration, $"Migrated market weapon categories for {migratedCount} listings.");
        }

        private static bool TryMapCategory(JToken categoryToken, out MarketCategoryType category)
        {
            category = MarketCategoryType.Invalid;

            if (categoryToken == null || categoryToken.Type != JTokenType.String)
                return false;

            switch (categoryToken.Value<string>())
            {
                case "2":
                case "Fin. Vibroblade":
                case "FinesseVibroblade":
                    category = MarketCategoryType.Vibroknife;
                    return true;
                case "4":
                case "Polearm":
                    category = MarketCategoryType.Spear;
                    return true;
                default:
                    return false;
            }
        }
    }
}
