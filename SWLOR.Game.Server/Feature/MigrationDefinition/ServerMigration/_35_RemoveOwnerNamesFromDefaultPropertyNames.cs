using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _35_RemoveOwnerNamesFromDefaultPropertyNames : ServerMigrationBase, IServerMigration
    {
        public int Version => 35;
        public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;

        public void Migrate()
        {
            var query = new DBQuery<WorldProperty>();
            var count = (int)DB.SearchCount(query);
            var properties = DB.Search(query.AddPaging(count, 0));
            var migratedCount = 0;

            foreach (var property in properties)
            {
                if (string.IsNullOrWhiteSpace(property.CustomName))
                    continue;

                var oldDefaultName = GetOldDefaultPropertyName(property);
                var newDefaultName = GetNewDefaultPropertyName(property);
                if (string.IsNullOrWhiteSpace(oldDefaultName) ||
                    string.IsNullOrWhiteSpace(newDefaultName) ||
                    !HasOwnerPrefixedDefaultName(property.CustomName, oldDefaultName))
                    continue;

                property.CustomName = newDefaultName;
                DB.Set(property);
                migratedCount++;
            }

            Log.Write(LogGroup.Migration, $"Removed owner names from {migratedCount} default property names.");
        }

        private static bool HasOwnerPrefixedDefaultName(string propertyName, string oldDefaultName)
        {
            var defaultSuffix = $"'s {oldDefaultName}";
            return propertyName.Length > defaultSuffix.Length &&
                   propertyName.EndsWith(defaultSuffix, StringComparison.Ordinal);
        }

        private static string GetOldDefaultPropertyName(WorldProperty property)
        {
            switch (property.PropertyType)
            {
                case PropertyType.Apartment:
                    return "Apartment";
                case PropertyType.Starship:
                    return "Starship";
                case PropertyType.City:
                    return "City";
                default:
                    return GetLayoutName(property.Layout);
            }
        }

        private static string GetNewDefaultPropertyName(WorldProperty property)
        {
            switch (property.PropertyType)
            {
                case PropertyType.Apartment:
                    return "Apartment";
                case PropertyType.Starship:
                    return "Starship";
                case PropertyType.City:
                    return "Player City";
                default:
                    return GetLayoutName(property.Layout);
            }
        }

        private static string GetLayoutName(PropertyLayoutType layout)
        {
            if (layout == PropertyLayoutType.Invalid)
                return string.Empty;

            return Property.GetLayoutByType(layout).Name;
        }
    }
}
