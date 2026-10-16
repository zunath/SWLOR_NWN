using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class RareLootBalanceTests
{
    private static readonly Dictionary<string, Blueprint> BlueprintCache = new(StringComparer.OrdinalIgnoreCase);
    private sealed record Blueprint(int BaseItem, Dictionary<(ItemPropertyType Type, int Subtype), int> Properties)
    {
        public int Amount(ItemPropertyType type) => Properties.Where(x => x.Key.Type == type).Sum(x => x.Value);
        public int Level => Amount(ItemPropertyType.RequiresSkill);
    }

    private static readonly Dictionary<ItemPropertyType, double> Weights = new()
    {
        [ItemPropertyType.Defense] = 1, [ItemPropertyType.HPBonus] = 1 / 8.0,
        [ItemPropertyType.FP] = 5 / 6.0, [ItemPropertyType.Stamina] = 5 / 6.0,
        [ItemPropertyType.Attack] = 2, [ItemPropertyType.ForceAttack] = 2, [ItemPropertyType.Evasion] = 2,
        [ItemPropertyType.CombatReadiness] = 1, [ItemPropertyType.FPRegen] = 2.5, [ItemPropertyType.STMRegen] = 2.5,
    };

    private static readonly Dictionary<EnhancementSubType, ItemPropertyType> EnhancementTypes = new()
    {
        [EnhancementSubType.DefensePhysical] = ItemPropertyType.Defense,
        [EnhancementSubType.DefenseForce] = ItemPropertyType.Defense,
        [EnhancementSubType.HP] = ItemPropertyType.HPBonus,
        [EnhancementSubType.FP] = ItemPropertyType.FP,
        [EnhancementSubType.Stamina] = ItemPropertyType.Stamina,
        [EnhancementSubType.Attack] = ItemPropertyType.Attack,
        [EnhancementSubType.ForceAttack] = ItemPropertyType.ForceAttack,
        [EnhancementSubType.Evasion] = ItemPropertyType.Evasion,
        [EnhancementSubType.CombatReadiness] = ItemPropertyType.CombatReadiness,
    };

    private static readonly List<RecipeDetail> Recipes = typeof(IRecipeListDefinition).Assembly.GetTypes()
        .Where(t => typeof(IRecipeListDefinition).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
        .SelectMany(t => ((IRecipeListDefinition)Activator.CreateInstance(t)!).BuildRecipes().Values)
        .Where(r => r.IsActive && r.EnhancementSlots > 0 && !LootStatRevisionCatalog.Changes.ContainsKey(r.Resref))
        .ToList();

    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        }
    }

    [Test]
    public void CorrectionsAreAdditiveSecondaryStatsAndMatchStampedBlueprints()
    {
        foreach (var (resref, changes) in RareLootStatRevisionCatalog.Changes)
        {
            var blueprint = Read(resref);
            foreach (var change in changes)
            {
                change.Type.Should().BeOneOf(ItemPropertyType.Attack, ItemPropertyType.ForceAttack,
                    ItemPropertyType.Evasion, ItemPropertyType.CombatReadiness);
                change.Current.Should().BeGreaterThan(change.Previous, resref);
                var previous = LootStatRevisionCatalog.Changes[resref]
                    .SingleOrDefault(x => x.Type == change.Type && x.Subtype == change.Subtype);
                change.Previous.Should().Be(previous.Current, $"{resref} follows the first revision exactly");
                blueprint.Amount(change.Type).Should().Be(change.Current, resref);
            }
            using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "Module", "uti", resref + ".uti.json")));
            json.RootElement.GetProperty("VarTable").GetProperty("value").EnumerateArray().Should().Contain(x =>
                x.GetProperty("Name").GetProperty("value").GetString() == RareLootStatRevisionCatalog.RevisionVariable &&
                x.GetProperty("Value").GetProperty("value").GetInt32() == RareLootStatRevisionCatalog.CurrentRevision, resref);
            foreach (var damage in LootStatRevisionCatalog.Changes[resref].Where(x => x.Type == ItemPropertyType.DMG))
                blueprint.Amount(ItemPropertyType.DMG).Should().Be(damage.Current, $"{resref} keeps its justified damage correction");
        }
    }

    [Test]
    public void RareWearablesFitSameLevelCraftedGearIncludingAllRecipeEnhancements()
    {
        var enhancements = Enhancements("aen_", ItemPropertyType.ArmorEnhancement);
        foreach (var resref in RareLootStatRevisionCatalog.Changes.Keys)
        {
            var rare = Read(resref);
            if (rare.Amount(ItemPropertyType.DMG) > 0)
                continue;
            var comparisons = Recipes.Where(r => r.EnhancementType == RecipeEnhancementType.Armor)
                .Select(r => (Recipe: r, Item: Read(r.Resref)))
                .Where(x => x.Item.BaseItem == rare.BaseItem && x.Item.Level <= rare.Level)
                .Select(x => Units(x.Item) + x.Recipe.EnhancementSlots * enhancements
                    .Where(e => e.Level <= x.Recipe.Level + 5 && EnhancementTypes.ContainsKey(e.Subtype))
                    .Max(e => Weights[EnhancementTypes[e.Subtype]] * e.Amount))
                .ToList();
            comparisons.Should().NotBeEmpty($"{resref} needs an eligible crafted comparison");
            Units(rare).Should().BeLessThanOrEqualTo(comparisons.Max() + 0.001,
                $"{resref} must fit the complete enhanced-crafted slot budget");
        }
    }

    [Test]
    public void RareWeaponsCompeteWithEnhancedCraftedGearForActivatedAttacks()
    {
        // Compare a +60 weapon ability with equal attacking/defending attributes and hit rates.
        // Every mixture of DMG and Attack enhancements is considered; bonuses are additive.
        // This protects an ability-focused niche without requiring drops to beat every auto-attack build.
        var enhancements = Enhancements("wen_", ItemPropertyType.WeaponEnhancement);
        foreach (var (resref, changes) in RareLootStatRevisionCatalog.Changes)
        {
            var rare = Read(resref);
            if (rare.Amount(ItemPropertyType.DMG) == 0)
                continue;
            var offense = changes.Any(x => x.Type == ItemPropertyType.ForceAttack)
                ? ItemPropertyType.ForceAttack : ItemPropertyType.Attack;
            var subtype = offense == ItemPropertyType.ForceAttack ? EnhancementSubType.ForceAttack : EnhancementSubType.Attack;
            var baseAttack = Stat.GetAttack(rare.Level, 20, 0);
            var comparisons = new List<double>();
            foreach (var recipe in Recipes.Where(r => r.EnhancementType == RecipeEnhancementType.Weapon))
            {
                var crafted = Read(recipe.Resref);
                if (crafted.BaseItem != rare.BaseItem || crafted.Level > rare.Level)
                    continue;
                var compatible = enhancements.Where(e => e.Level <= recipe.Level + 5).ToList();
                var damage = compatible.Where(e => e.Subtype == EnhancementSubType.DMG).Max(e => e.Amount);
                var attack = compatible.Where(e => e.Subtype == subtype).Max(e => e.Amount);
                for (var damageSlots = 0; damageSlots <= recipe.EnhancementSlots; damageSlots++)
                    comparisons.Add((crafted.Amount(ItemPropertyType.DMG) + 60.0 + damageSlots * damage) *
                        (baseAttack + crafted.Amount(offense) + (recipe.EnhancementSlots - damageSlots) * attack));
            }
            comparisons.Should().NotBeEmpty($"{resref} needs an eligible crafted comparison");
            var magnitude = (rare.Amount(ItemPropertyType.DMG) + 60.0) *
                (baseAttack + rare.Amount(offense)) * (1 + rare.Amount(ItemPropertyType.CombatReadiness) / 100.0);
            (magnitude / comparisons.Max()).Should().BeInRange(0.99, 1.20,
                $"{resref} should compete with enhanced crafting without an excessive ability-damage premium");
        }
    }

    [Test]
    public void ZeroStateKeepsItsDamageAndRareBonuses()
    {
        var rare = Read("stw_zerostate");
        rare.Amount(ItemPropertyType.DMG).Should().Be(22);
        rare.Amount(ItemPropertyType.Attack).Should().Be(12);
        rare.Amount(ItemPropertyType.CombatReadiness).Should().Be(4);
        rare.Amount(ItemPropertyType.Delay).Should().Be(23);
        rare.Level.Should().Be(40);
    }

    private static double Units(Blueprint item) =>
        item.Properties.Where(x => x.Key.Type != ItemPropertyType.Defense || x.Key.Subtype is 1 or 2)
            .Sum(x => Weights.GetValueOrDefault(x.Key.Type) * x.Value);

    private static List<(int Level, EnhancementSubType Subtype, int Amount)> Enhancements(string prefix, ItemPropertyType type) =>
        Directory.EnumerateFiles(Path.Combine(Root, "Module", "uti"), prefix + "*.uti.json")
            .Select(p => Read(Path.GetFileName(p)[..^".uti.json".Length]))
            .SelectMany(p => p.Properties.Where(x => x.Key.Type == type)
                .Select(x => (p.Amount(ItemPropertyType.EnhancementLevel), (EnhancementSubType)x.Key.Subtype, x.Value))).ToList();

    private static Blueprint Read(string resref)
    {
        if (BlueprintCache.TryGetValue(resref, out var cached))
            return cached;
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "Module", "uti", resref + ".uti.json")));
        var root = json.RootElement;
        var properties = root.GetProperty("PropertiesList").GetProperty("value").EnumerateArray()
            .GroupBy(p => ((ItemPropertyType)p.GetProperty("PropertyName").GetProperty("value").GetInt32(),
                p.GetProperty("Subtype").GetProperty("value").GetInt32()))
            .ToDictionary(g => g.Key, g => g.Sum(p => p.GetProperty("CostValue").GetProperty("value").GetInt32()));
        var blueprint = new Blueprint(root.GetProperty("BaseItem").GetProperty("value").GetInt32(), properties);
        BlueprintCache.Add(resref, blueprint);
        return blueprint;
    }
}
