using System.Collections;
using System.Reflection;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.CharacterizationTests.Definitions;

/// <summary>
/// Reaches the internal <c>BlueprintBonuses</c> class by reflection (the production assembly does not expose it to tests)
/// and presents its bonus tables and weighted pick as plain values. The class has no engine dependencies; the pick uses
/// the game's global <see cref="Random"/>, so a fixture that draws must seed it and be <c>[NonParallelizable]</c>.
/// </summary>
internal sealed class BlueprintBonusSource
{
    private const string TypeName = "SWLOR.Game.Server.Service.CraftService.BlueprintBonuses";
    private static readonly Type BonusesType = typeof(Craft).Assembly.GetType(TypeName, throwOnError: true)!;
    private static readonly FieldInfo TablesField = BonusesType.GetField("_bonusesByEnhancementType", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly MethodInfo PickMethod = BonusesType.GetMethod("PickBonus")!;

    private readonly object _instance = Activator.CreateInstance(BonusesType)!;

    /// <summary>One bonus a blueprint can roll.</summary>
    public readonly record struct Bonus(int Weight, EnhancementSubType Type, int Amount, CombatDamageType DamageType);

    /// <summary>The bonuses one table (enhancement type, crafting flag, tier) holds, in table order.</summary>
    public readonly record struct Table(RecipeEnhancementType EnhancementType, bool IsCraftingItem, int Tier, IReadOnlyList<Bonus> Bonuses);

    /// <summary>Every table, sorted by enhancement type id, then the crafting flag (false first), then tier.</summary>
    public IReadOnlyList<Table> Tables()
    {
        var tables = new List<Table>();
        var byType = (IDictionary)TablesField.GetValue(_instance)!;
        foreach (DictionaryEntry typeEntry in byType)
        {
            foreach (DictionaryEntry flagEntry in (IDictionary)typeEntry.Value!)
            {
                foreach (DictionaryEntry tierEntry in (IDictionary)flagEntry.Value!)
                {
                    var bonuses = ((IEnumerable)tierEntry.Value!).Cast<object>().Select(ToBonus).ToArray();
                    tables.Add(new Table((RecipeEnhancementType)typeEntry.Key, (bool)flagEntry.Key, (int)tierEntry.Key, bonuses));
                }
            }
        }

        return tables
            .OrderBy(table => DefinitionCell.Id(table.EnhancementType))
            .ThenBy(table => table.IsCraftingItem)
            .ThenBy(table => table.Tier)
            .ToArray();
    }

    /// <summary>The table's position of the bonus a weighted pick returned, or null when the pick returned nothing (no such table).</summary>
    public int? PickIndex(RecipeEnhancementType enhancementType, int tier, bool isCraftingItem)
    {
        var picked = PickMethod.Invoke(_instance, new object[] { enhancementType, tier, isCraftingItem });
        if (picked == null)
            return null;

        var byType = (IDictionary)TablesField.GetValue(_instance)!;
        var set = (IList)((IDictionary)((IDictionary)byType[enhancementType]!)[isCraftingItem]!)[tier]!;
        for (var index = 0; index < set.Count; index++)
        {
            if (ReferenceEquals(set[index], picked))
                return index;
        }

        throw new InvalidOperationException("PickBonus returned a bonus that is not in the table it picked from.");
    }

    private static Bonus ToBonus(object bonus)
    {
        var type = bonus.GetType();
        return new Bonus(
            (int)type.GetProperty("Weight")!.GetValue(bonus)!,
            (EnhancementSubType)type.GetProperty("Type")!.GetValue(bonus)!,
            (int)type.GetProperty("Amount")!.GetValue(bonus)!,
            (CombatDamageType)type.GetProperty("DamageType")!.GetValue(bonus)!);
    }
}
