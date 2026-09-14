using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Tests.Perks;

public class CombatDeflectionBudgetTests
{
    private static readonly string[] WeaponCategoryPrefixes =
    {
        "Vibroblade",
        "Vibroknife",
        "Lightsaber",
        "Heavy Vibroblade",
        "Spear",
        "Twin Blade",
        "Saberstaff",
        "Katar",
        "Staff",
        "Pistol",
        "Rifle",
        "Throwing",
    };

    private static readonly Dictionary<(PerkType Perk, StatType Stat), int> DynamicStatBudgetValues = new()
    {
        [(PerkType.UnbreakableWill, StatType.MeleeDeflection)] = 8,
    };

    [TestCase(StatType.MeleeDeflection)]
    [TestCase(StatType.RangedDeflection)]
    public void PermanentWeaponDeflectionSources_StayBelowIndependentDefaultCap(StatType deflectionStat)
    {
        var sources = WeaponStatSources(deflectionStat)
            .Where(source => source.Value > 0)
            .ToArray();

        sources.Sum(source => source.Value).Should().BeLessThan(
            50,
            $"permanent {deflectionStat} should not reach its soft cap before temporary effects");
        sources.Where(source => source.Value > 15).Should().BeEmpty(
            "large weapon deflection spikes should come from temporary effects, not always-on perk levels");
    }

    [Test]
    public void AlwaysOnWeaponCriticalRateSources_StayBelowCritCapBeforeStancesAndSupport()
    {
        var sources = WeaponStatSources(StatType.CriticalRatePercentAdjustment)
            .Where(source => source.Value > 0)
            .ToArray();

        sources.Sum(source => source.Value).Should().BeLessThan(
            50,
            "always-on weapon crit should leave room for stance, support, and temporary setup windows");
    }

    [Test]
    public void MeleeRangedShieldDeflectionAndGuard_BudgetsRemainMechanicallySeparate()
    {
        var failures = new List<string>();

        foreach (var source in WeaponPerkLevels())
        {
            var stats = source.Level.StatBonuses
                .Select(bonus => bonus.Stat)
                .ToHashSet();

            var hasWeaponDeflection = stats.Contains(StatType.MeleeDeflection) ||
                                      stats.Contains(StatType.RangedDeflection);
            if (hasWeaponDeflection &&
                (stats.Contains(StatType.ShieldDeflection) || stats.Contains(StatType.Guard)))
            {
                failures.Add($"{source.Perk.Name} level {source.LevelNumber} mixes weapon deflection with Shield Deflection or Guard");
            }

            if (stats.Contains(StatType.MeleeDeflection) && stats.Contains(StatType.RangedDeflection))
            {
                failures.Add($"{source.Perk.Name} level {source.LevelNumber} mixes Melee and Ranged Deflection");
            }

            if (stats.Contains(StatType.ShieldDeflection) && stats.Contains(StatType.Guard))
            {
                failures.Add($"{source.Perk.Name} level {source.LevelNumber} mixes Shield Deflection with Guard");
            }
        }

        failures.Should().BeEmpty(
            "Melee Deflection, Ranged Deflection, Shield Deflection, and Guard are separate budget lanes");
    }

    [Test]
    public void DeflectionTriggeredTemporaryWindows_RespectMinimumDuration()
    {
        var fieldNames = new[]
        {
            "DeflectionEvasionBoostDurationSeconds",
            "DeflectionEnmityBoostDurationSeconds",
            "DeflectionDefenseBoostDurationSeconds"
        };

        foreach (var fieldName in fieldNames)
        {
            var duration = (float)typeof(Stat)
                .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)!
                .GetRawConstantValue()!;

            duration.Should().BeGreaterThanOrEqualTo(30f);
        }
    }

    private static IEnumerable<StatSource> WeaponStatSources(StatType statType)
    {
        return WeaponPerkLevels()
            .SelectMany(source => source.Level.StatBonuses
                .Where(bonus => bonus.Stat == statType)
                .Select(bonus => new StatSource(
                    source.Perk.Name,
                    source.LevelNumber,
                    statType,
                    CalculateBudgetValue(source.Type, bonus))));
    }

    private static int CalculateBudgetValue(PerkType perkType, PerkStatBonus bonus)
    {
        return DynamicStatBudgetValues.TryGetValue((perkType, bonus.Stat), out var budgetValue)
            ? budgetValue
            : bonus.Calculate(0);
    }

    private static IEnumerable<PerkLevelSource> WeaponPerkLevels()
    {
        foreach (var (type, perk) in BuildPerksWithout2daLookup())
        {
            if (!IsWeaponCategory(perk.Category))
                continue;

            foreach (var (levelNumber, level) in perk.PerkLevels)
            {
                yield return new PerkLevelSource(type, perk, levelNumber, level);
            }
        }
    }

    private static bool IsWeaponCategory(PerkCategoryType category)
    {
        var name = GetCategoryName(category);
        return WeaponCategoryPrefixes.Any(prefix =>
            name.StartsWith(prefix + " - ", StringComparison.Ordinal));
    }

    private static string GetCategoryName(PerkCategoryType category)
    {
        var field = typeof(PerkCategoryType).GetField(category.ToString())!;
        var attribute = (PerkCategoryAttribute)field
            .GetCustomAttributes(typeof(PerkCategoryAttribute), false)
            .Single();

        return attribute.Name;
    }

    private static IReadOnlyCollection<(PerkType Type, PerkDetail Detail)> BuildPerksWithout2daLookup()
    {
        var result = new List<(PerkType Type, PerkDetail Detail)>();
        var definitionTypes = typeof(IPerkListDefinition).Assembly
            .GetTypes()
            .Where(x => !x.IsAbstract && typeof(IPerkListDefinition).IsAssignableFrom(x))
            .OrderBy(x => x.FullName)
            .ToArray();

        foreach (var definitionType in definitionTypes)
        {
            var definition = Activator.CreateInstance(definitionType)!;
            foreach (var method in definitionType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                         .Where(x => x.ReturnType == typeof(void) && x.GetParameters().Length == 0 && !x.Name.Contains('<'))
                         .OrderBy(x => x.MetadataToken))
            {
                method.Invoke(definition, null);
            }

            var builder = definitionType
                .GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(definition)!;

            var perks = (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
                .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(builder)!;

            result.AddRange(perks.Select(x => (x.Key, x.Value)));
        }

        return result;
    }

    private sealed record PerkLevelSource(PerkType Type, PerkDetail Perk, int LevelNumber, PerkLevel Level);

    private sealed record StatSource(string PerkName, int LevelNumber, StatType Stat, int Value);
}
