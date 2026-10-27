using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.CharacterizationTests.Definitions;

/// <summary>
/// Builds every loot table the way <c>Loot.RegisterLootTables</c> does at module load: each
/// <see cref="ILootTableDefinition"/> builds its tables. The engine is not involved; item spawn actions are only
/// stored, never run.
/// </summary>
internal static class LootTableDefinitionSource
{
    /// <summary>A loot table with its id and the definition class that built it.</summary>
    public readonly record struct Entry(string Id, LootTable Table, string Definition);

    /// <summary>All tables sorted by id (ordinal). Fails when two definitions register the same id.</summary>
    public static IReadOnlyList<Entry> Load()
    {
        var tables = new Dictionary<string, Entry>(StringComparer.Ordinal);

        foreach (var definitionType in DefinitionTypes())
        {
            var definition = (ILootTableDefinition)Activator.CreateInstance(definitionType, nonPublic: true)!;
            foreach (var (id, table) in definition.BuildLootTables())
            {
                if (tables.TryGetValue(id, out var existing))
                    throw new InvalidOperationException($"Loot table {id} is built by both {existing.Definition} and {definitionType.FullName}.");

                tables[id] = new Entry(id, table, definitionType.FullName!);
            }
        }

        return tables.Values.OrderBy(entry => entry.Id, StringComparer.Ordinal).ToArray();
    }

    /// <summary>The names of the item snapshot files: tables are grouped by the first letter of their id.</summary>
    public static readonly string[] ItemGroupNames = { "A-C", "D-H", "I-M", "N-S", "T-Z" };

    /// <summary>The item snapshot group a table id belongs to; ids before C go in the first group and ids after S in the last.</summary>
    public static string ItemGroupOf(string tableId)
    {
        var first = char.ToUpperInvariant(tableId[0]);
        if (first <= 'C')
            return ItemGroupNames[0];
        if (first <= 'H')
            return ItemGroupNames[1];
        if (first <= 'M')
            return ItemGroupNames[2];
        return first <= 'S' ? ItemGroupNames[3] : ItemGroupNames[4];
    }

    /// <summary>Every loot table definition class, in a fixed order.</summary>
    public static IReadOnlyList<Type> DefinitionTypes()
    {
        return typeof(ILootTableDefinition).Assembly
            .GetTypes()
            .Where(type => typeof(ILootTableDefinition).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
    }
}
