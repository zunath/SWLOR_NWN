using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.PlayerMarketService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _23_CollapseMarketArmorCategories : ServerMigrationBase, IServerMigration
    {
        public int Version => 23;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;

        public void Migrate()
        {
            var query = new DBQuery<MarketItem>();
            var count = (int)DB.SearchCount(query);
            var items = DB.SearchRawJson(query.AddPaging(count, 0));

            foreach (var rawItem in items)
            {
                var jObject = JObject.Parse(rawItem);
                var categoryToken = jObject[nameof(MarketItem.Category)];

                if (!TryMapArmorCategory(categoryToken, out var category))
                    continue;

                jObject[nameof(MarketItem.Category)] = (int)category;

                var item = jObject.ToObject<MarketItem>();
                DB.Set(item);
            }
        }

        private static bool TryMapArmorCategory(JToken categoryToken, out MarketCategoryType category)
        {
            category = MarketCategoryType.Invalid;

            if (categoryToken == null)
                return false;

            return categoryToken.Type == JTokenType.Integer
                ? TryMapArmorCategory(categoryToken.Value<int>(), out category)
                : TryMapArmorCategory(categoryToken.Value<string>(), out category);
        }

        private static bool TryMapArmorCategory(int categoryId, out MarketCategoryType category)
        {
            return TryMapArmorCategory(categoryId.ToString(), out category);
        }

        private static bool TryMapArmorCategory(string categoryNameOrId, out MarketCategoryType category)
        {
            category = MarketCategoryType.Invalid;

            if (string.IsNullOrWhiteSpace(categoryNameOrId))
                return false;

            switch (categoryNameOrId)
            {
                case "16":
                case "Breastplate":
                case "Tunic":
                    category = MarketCategoryType.Armor;
                    return true;
                case "17":
                case "21":
                case "Helmet":
                case "Cap":
                    category = MarketCategoryType.Helmet;
                    return true;
                case "18":
                case "22":
                case "Bracer":
                case "Glove":
                    category = MarketCategoryType.Glove;
                    return true;
                case "19":
                case "23":
                case "Legging":
                case "Boot":
                    category = MarketCategoryType.Boot;
                    return true;
                default:
                    if (!Enum.TryParse(categoryNameOrId, out MarketCategoryType parsedCategory) ||
                        !Enum.IsDefined(typeof(MarketCategoryType), parsedCategory))
                    {
                        return false;
                    }

                    category = parsedCategory;
                    return false;
            }
        }
    }
}
