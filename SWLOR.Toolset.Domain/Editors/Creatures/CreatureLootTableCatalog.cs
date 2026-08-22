using System.Reflection;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Toolset.Domain.Editors.Creatures
{
    /// <summary>Registered loot tables projected from their side-effect-free definitions.</summary>
    public static class CreatureLootTableCatalog
    {
        public static IReadOnlyList<CreatureLootTableInfo> Build()
        {
            var result = new Dictionary<string, CreatureLootTableInfo>(StringComparer.Ordinal);
            foreach (var type in typeof(ILootTableDefinition).Assembly.GetTypes()
                         .Where(type => typeof(ILootTableDefinition).IsAssignableFrom(type) &&
                                        !type.IsAbstract && !type.IsInterface))
            {
                try
                {
                    if (Activator.CreateInstance(type) is not ILootTableDefinition definition)
                        continue;

                    foreach (var (id, table) in definition.BuildLootTables())
                    {
                        result[id] = new CreatureLootTableInfo(
                            id,
                            Humanize(id),
                            table.IsRare,
                            table.Select(item => new CreatureLootTableItemInfo(
                                item.Resref,
                                item.Weight,
                                item.MaxQuantity,
                                item.IsRare)).ToList(),
                            type.Name);
                    }
                }
                catch (Exception ex) when (ex is TargetInvocationException or InvalidOperationException or ArgumentException)
                {
                }
            }

            return result.Values
                .OrderBy(table => table.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string Humanize(string value) =>
            System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(
                value.Replace('_', ' ').ToLowerInvariant());
    }
}
