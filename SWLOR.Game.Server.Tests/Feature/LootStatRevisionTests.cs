using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition;
using SWLOR.Game.Server.Service.LootService;
using SWLOR.Game.Server.Service.SlicingService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class LootStatRevisionTests
{
    private const int RequiresSkill = (int)ItemPropertyType.RequiresSkill;
    private const int Defense = (int)ItemPropertyType.Defense;

    // Budget units: one level-45 enhancement slot buys 5 Defense, 40 HP or 6 FP/STM. Attack, Force Attack
    // and Evasion cost double because each point moves damage dealt or taken more than a point of Defense.
    private static readonly Dictionary<(int Type, int Subtype), double> UnitWeights = new()
    {
        [(Defense, 1)] = 1, [(Defense, 2)] = 1, [((int)ItemPropertyType.HPBonus, 0)] = 1 / 8.0,
        [((int)ItemPropertyType.FP, 0)] = 5 / 6.0, [((int)ItemPropertyType.Stamina, 0)] = 5 / 6.0,
        [((int)ItemPropertyType.CombatReadiness, 0)] = 1, [((int)ItemPropertyType.Attack, 0)] = 2,
        [((int)ItemPropertyType.ForceAttack, 0)] = 2, [((int)ItemPropertyType.Evasion, 0)] = 2,
        [((int)ItemPropertyType.FPRegen, 0)] = 2.5, [((int)ItemPropertyType.STMRegen, 0)] = 2.5,
    };

    // Level-50 budget per slot, matching crafted Chaos/Immortal/Magus gear.
    private static readonly Dictionary<BaseItem, double> SlotBudgets = new()
    {
        [BaseItem.Armor] = 68, [BaseItem.Helmet] = 52, [BaseItem.Gloves] = 50, [BaseItem.Bracer] = 50,
        [BaseItem.Belt] = 30, [BaseItem.Cloak] = 31, [BaseItem.Ring] = 30, [BaseItem.Amulet] = 31,
        [BaseItem.Boots] = 49, [BaseItem.LargeShield] = 32,
    };

    // Weapon DMG by base item and required level: the highest crafted, store or loot value from before the
    // Combat Upgrade at each level.
    private static readonly Dictionary<int, SortedDictionary<int, int>> WeaponDamageCurves = new()
    {
        [(int)BaseItem.Longsword] = Curve(6, 7, 11, 11, 15, 15, 18, 19, 23, 24),
        [(int)BaseItem.Rifle] = Curve(8, 11, 22, 22, 29, 29, 35, 35, 39, 40),
        [(int)BaseItem.TwoBladedSword] = Curve(6, 7, 11, 11, 15, 15, 18, 19, 23, 24),
        [(int)BaseItem.GreatSword] = Curve(9, 12, 22, 22, 35, 35, 38, 38, 42, 44),
        [(int)BaseItem.Dagger] = Curve(7, 7, 10, 10, 15, 15, 18, 18, 21, 22),
        [(int)BaseItem.QuarterStaff] = Curve(6, 7, 11, 11, 15, 15, 18, 19, 23, 24),
        [(int)BaseItem.ShortSpear] = Curve(8, 11, 21, 21, 27, 33, 33, 40, 41, 42),
        [(int)BaseItem.Shuriken] = Curve(5, 5, 8, 8, 11, 11, 13, 14, 19, 19),
        // Pistol blueprints use base item 61.
        [(int)BaseItem.Sling] = Curve(6, 7, 11, 11, 15, 15, 17, 22, 22, 23),
        [(int)BaseItem.Katar] = Curve(8, 8, 10, 10, 13, 13, 14, 15, 18, 19),
    };

    [Test]
    public void Catalog_MatchesRevisedBlueprints()
    {
        var root = FindRepositoryRoot();
        var failures = new List<string>();
        foreach (var (resref, changes) in LootStatRevisionCatalog.Changes)
        {
            var path = Path.Combine(root.FullName, "Module", "uti", $"{resref}.uti.json");
            if (!File.Exists(path))
            {
                failures.Add($"{resref}: blueprint is missing");
                continue;
            }

            using var uti = JsonDocument.Parse(File.ReadAllText(path));
            var properties = ReadProperties(uti.RootElement);
            foreach (var change in changes)
            {
                var subtype = change.Type == ItemPropertyType.Defense ? change.Subtype : 0;
                properties.TryGetValue(((int)change.Type, subtype), out var value);
                if (value != change.Current)
                    failures.Add($"{resref}: {change.Type} is {value}, catalog expects {change.Current}");
                if (change.Previous == change.Current)
                    failures.Add($"{resref}: {change.Type} has no change");
            }

            if (ReadLocalInt(uti.RootElement, LootStatRevisionMigration.RevisionVariable) != LootStatRevisionMigration.CurrentRevision)
                failures.Add($"{resref}: blueprint must carry {LootStatRevisionMigration.RevisionVariable} = {LootStatRevisionMigration.CurrentRevision}");
        }

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    [Test]
    public void RevisionMarker_OnlyAppearsOnCatalogedBlueprints()
    {
        var root = FindRepositoryRoot();
        var stamped = Directory.EnumerateFiles(Path.Combine(root.FullName, "Module", "uti"), "*.uti.json")
            .Where(path => File.ReadAllText(path).Contains(LootStatRevisionMigration.RevisionVariable))
            .Select(path => Path.GetFileName(path)[..^".uti.json".Length]);

        stamped.Should().OnlyContain(resref => LootStatRevisionCatalog.Changes.ContainsKey(resref),
            "an item built from a stamped blueprint is never migrated, so the catalog must describe it");
    }

    [Test]
    public void LevelFortyFiveLoot_StaysWithinItsSlotBudget()
    {
        var root = FindRepositoryRoot();
        var failures = new List<string>();
        foreach (var resref in GetLootAndSalvageResrefs())
        {
            var path = Path.Combine(root.FullName, "Module", "uti", $"{resref}.uti.json");
            if (!File.Exists(path))
                continue;

            using var uti = JsonDocument.Parse(File.ReadAllText(path));
            var properties = ReadProperties(uti.RootElement);
            if (RequiredLevel(properties) != 45)
                continue;

            var baseItem = uti.RootElement.GetProperty("BaseItem").GetProperty("value").GetInt32();
            if (properties.TryGetValue(((int)ItemPropertyType.DMG, 0), out var damage))
            {
                if (!WeaponDamageCurves.TryGetValue(baseItem, out var curve))
                {
                    failures.Add($"{resref}: base item {baseItem} has no weapon damage reference");
                    continue;
                }

                if (damage > curve[50])
                    failures.Add($"{resref}: DMG {damage} exceeds the level-50 reference {curve[50]}");
                foreach (var type in new[] { ItemPropertyType.Attack, ItemPropertyType.ForceAttack })
                    if (Value(properties, type) > 5)
                        failures.Add($"{resref}: weapon {type} {Value(properties, type)} exceeds 5");
                continue;
            }

            if (!SlotBudgets.TryGetValue((BaseItem)baseItem, out var budget))
                continue;
            foreach (var type in new[] { ItemPropertyType.Attack, ItemPropertyType.ForceAttack, ItemPropertyType.Evasion })
                if (Value(properties, type) > 8)
                    failures.Add($"{resref}: {type} {Value(properties, type)} exceeds 8");
            var units = properties.Sum(x => UnitWeights.TryGetValue(x.Key, out var weight) ? weight * x.Value : 0);
            if (units > budget + 0.5)
                failures.Add($"{resref}: {units:0.0} budget units exceed the {(BaseItem)baseItem} budget of {budget}");
        }

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    [Test]
    public void SlicingRewardWeapons_FollowTheLevelDamageCurve()
    {
        var root = FindRepositoryRoot();
        var failures = new List<string>();
        foreach (var resref in SlicingRewardCatalog.Entries.Select(x => x.Resref).Distinct())
        {
            var path = Path.Combine(root.FullName, "Module", "uti", $"{resref}.uti.json");
            if (!File.Exists(path))
                continue;

            using var uti = JsonDocument.Parse(File.ReadAllText(path));
            var properties = ReadProperties(uti.RootElement);
            if (!properties.TryGetValue(((int)ItemPropertyType.DMG, 0), out var damage))
                continue;

            var baseItem = uti.RootElement.GetProperty("BaseItem").GetProperty("value").GetInt32();
            var level = RequiredLevel(properties) ?? 0;
            WeaponDamageCurves.Should().ContainKey(baseItem, $"{resref} needs a weapon damage reference");
            var reference = WeaponDamageCurves[baseItem].Last(x => x.Key <= level).Value;
            if (damage > reference)
                failures.Add($"{resref}: DMG {damage} exceeds the level-{level} reference {reference}");
            if (Value(properties, ItemPropertyType.Attack) > 3)
                failures.Add($"{resref}: Attack {Value(properties, ItemPropertyType.Attack)} exceeds 3");
            if (Value(properties, ItemPropertyType.CombatReadiness) > 3)
                failures.Add($"{resref}: Combat Readiness {Value(properties, ItemPropertyType.CombatReadiness)} exceeds 3");
        }

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    private static SortedDictionary<int, int> Curve(params int[] damageByLevel)
    {
        var levels = new[] { 0, 5, 10, 15, 20, 25, 30, 35, 40, 50 };
        return new SortedDictionary<int, int>(levels.Zip(damageByLevel).ToDictionary(x => x.First, x => x.Second));
    }

    private static IEnumerable<string> GetLootAndSalvageResrefs()
    {
        var resrefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var type in typeof(ILootTableDefinition).Assembly.GetTypes()
                     .Where(type => typeof(ILootTableDefinition).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface))
        {
            foreach (var (_, table) in ((ILootTableDefinition)Activator.CreateInstance(type)!).BuildLootTables())
                foreach (var item in table)
                    resrefs.Add(item.Resref);
        }

        foreach (var recipe in new SalvagedFieldGearRecipes().BuildRecipes().Values)
            resrefs.Add(recipe.Resref);
        return resrefs;
    }

    private static Dictionary<(int Type, int Subtype), int> ReadProperties(JsonElement uti)
    {
        var properties = new Dictionary<(int Type, int Subtype), int>();
        foreach (var property in uti.GetProperty("PropertiesList").GetProperty("value").EnumerateArray())
        {
            var type = property.GetProperty("PropertyName").GetProperty("value").GetInt32();
            var subtype = type == Defense ? property.GetProperty("Subtype").GetProperty("value").GetInt32() : 0;
            var value = property.GetProperty("CostValue").GetProperty("value").GetInt32();
            if (type == RequiresSkill && properties.ContainsKey((type, 0)))
                continue;
            properties[(type, subtype)] = properties.GetValueOrDefault((type, subtype)) + value;
        }

        return properties;
    }

    private static int? RequiredLevel(Dictionary<(int Type, int Subtype), int> properties)
        => properties.TryGetValue((RequiresSkill, 0), out var level) ? level : null;

    private static int Value(Dictionary<(int Type, int Subtype), int> properties, ItemPropertyType type)
        => properties.GetValueOrDefault(((int)type, 0));

    private static int ReadLocalInt(JsonElement uti, string name)
    {
        if (!uti.TryGetProperty("VarTable", out var table))
            return 0;
        foreach (var variable in table.GetProperty("value").EnumerateArray())
            if (variable.GetProperty("Name").GetProperty("value").GetString() == name)
                return variable.GetProperty("Value").GetProperty("value").GetInt32();
        return 0;
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull("the repository root should be discoverable from the test directory");
        return directory!;
    }
}
