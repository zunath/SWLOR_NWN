using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Tests.Perks;

/// <summary>
/// Defensive procs fire once per incoming hit, so every triggered resource restore is paired with
/// a cooldown stat and consumes it through <c>TryUseStatTrigger</c>. Guard rolls on every landed
/// physical hit from every attacker, which makes the gate load-bearing rather than cosmetic.
/// </summary>
public class TriggeredResourceRestoreGateTests
{
    // Restores that fire off an incoming attack are limited by the attacker count, not by the
    // defender's own rotation, so each one is gated by a cooldown or by a proc chance. Guard was
    // the only member of this family with neither.
    private static readonly (string Method, string Gate)[] DefensiveRestoreGates =
    {
        ("private static void ApplyGuardedHitRecovery(", "TryUseStatTrigger"),
        ("public static void ApplyMeleeDamageTakenEffects(", "StatType.MeleeDamageTakenStaminaRestoreChance"),
        ("private static void ApplyForceAbilityEvadedEffects(", "TryUseStatTrigger")
    };

    [TestCaseSource(nameof(DefensiveRestoreGates))]
    public void DefensiveResourceRestores_AreRateLimited((string Method, string Gate) usage)
    {
        var combat = ReadSource("SWLOR.Game.Server", "Service", "Combat.cs");
        ExtractMethod(combat, usage.Method).Should().Contain(usage.Gate,
            $"{usage.Method} fires once per incoming hit and must not scale freely with attacker count");
    }

    [Test]
    public void DeflectionResourceRestores_AreRateLimited()
    {
        var stat = ReadSource("SWLOR.Game.Server", "Service", "Stat.cs");
        var deflection = ExtractMethod(stat, "public static void ApplyDeflectionEffectsNative(");
        deflection.Should().Contain("Combat.TryUseStatTrigger(creatureId, staminaRestoreStat, staminaRestoreCooldown)");
        deflection.Should().Contain("Combat.TryUseStatTrigger(creatureId, fpRestoreStat, fpRestoreCooldown)");
        deflection.Should().Contain("recastReductionCooldown");
        deflection.Should().Contain("StatType.DeflectionRecastReductionCooldownSeconds");
        deflection.Should().MatchRegex(
            @"TryUseStatTrigger\(\s*creatureId,\s*StatType\.DeflectionRecastReductionSeconds,\s*recastReductionCooldown\s*\)",
            "a cooldown reduction on the same trigger outruns the cooldown it shortens without a gate");
    }

    [Test]
    public void ShieldTraining_GatesItsCooldownReductionLikeItsDeflectionSiblings()
    {
        var perk = Perk<VibrobladePerkDefinition>("ShieldTraining", PerkType.ShieldTraining);

        // The payload is equipment-conditional, so evaluate the shield-equipped branch by shape.
        var level = perk.PerkLevels[1];
        level.StatBonuses.Select(x => x.Stat).Should().Contain(StatType.DeflectionRecastReductionCooldownSeconds,
            "Shield Training shares Alacrity's shield-deflect trigger and must share its rate limit");
        level.Description.Should().Contain("once every 6 seconds");

        Stat.GetStatTypeAggregation(StatType.DeflectionRecastReductionCooldownSeconds)
            .Should().Be(StatTypeAggregation.Maximum);
        Stat.GetStatTypeAggregation(StatType.DeflectionRecastReductionGroupId)
            .Should().Be(StatTypeAggregation.Maximum,
                "the group id selects a RecastGroup and must never be summed across sources");
    }

    [Test]
    public void GuardStaminaRestore_IsGatedAtTheSameRateAsItsDefensiveSiblings()
    {
        var combat = ReadSource("SWLOR.Game.Server", "Service", "Combat.cs");
        var recovery = ExtractMethod(combat, "private static void ApplyGuardedHitRecovery(");
        recovery.Should().Contain("StatType.GuardStaminaRestoreCooldownSeconds");
        recovery.Should().Contain("TryUseStatTrigger(defender, StatType.GuardStaminaRestore, cooldown)");

        Stat.GetStatTypeAggregation(StatType.GuardStaminaRestoreCooldownSeconds)
            .Should().Be(StatTypeAggregation.Maximum,
                "two Iron Guard perks each declare the 6-second gate; the gate must not become 12 seconds");
    }

    [Test]
    public void IronGuardStaminaRestore_CarriesForwardAndDeclaresItsGate()
    {
        var perk = Perk<KatarPerkDefinition>("IronGuardTraining", PerkType.IronGuardTraining);

        foreach (var rank in new[] { 2, 3 })
        {
            Bonus(perk, rank, StatType.GuardStaminaRestore).Should().Be(2,
                $"Iron Guard Training rank {rank} restores 2 STM on a guarded hit");
            Bonus(perk, rank, StatType.GuardStaminaRestoreCooldownSeconds).Should().Be(6,
                $"Iron Guard Training rank {rank} gates the restore at 6 seconds");
        }

        Bonus(perk, 1, StatType.GuardStaminaRestore).Should().Be(0,
            "rank 1 grants Guard chance only");

        var grip = Perk<KatarPerkDefinition>("ImpenetrableGrip", PerkType.ImpenetrableGrip);
        Bonus(grip, 1, StatType.GuardStaminaRestore).Should().Be(4);
        Bonus(grip, 1, StatType.GuardStaminaRestoreCooldownSeconds).Should().Be(6);
    }

    [Test]
    public void EveryPerkGrantingGuardStaminaRestore_AlsoGrantsItsCooldown()
    {
        var offenders = new List<string>();
        foreach (var (type, detail) in AllPerks())
        {
            if (!detail.IsActive) continue;
            foreach (var (rank, level) in detail.PerkLevels)
            {
                var restore = level.StatBonuses
                    .Where(x => x.Stat == StatType.GuardStaminaRestore)
                    .Sum(x => x.Calculate(0));
                if (restore <= 0) continue;

                var cooldown = level.StatBonuses
                    .Where(x => x.Stat == StatType.GuardStaminaRestoreCooldownSeconds)
                    .Select(x => x.Calculate(0))
                    .DefaultIfEmpty(0)
                    .Max();
                if (cooldown <= 0)
                    offenders.Add($"{detail.Category}/{type} rank {rank}");
            }
        }

        offenders.Should().BeEmpty(
            "an ungated Guard restore scales with the number of attackers: " + string.Join(", ", offenders));
    }

    private static int Bonus(PerkDetail perk, int rank, StatType stat) =>
        perk.PerkLevels[rank].StatBonuses.Where(x => x.Stat == stat).Sum(x => x.Calculate(0));

    private static PerkDetail Perk<T>(string method, PerkType type) where T : new()
    {
        var definition = new T();
        typeof(T).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(definition, null);
        var builder = typeof(T).GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(definition)!;
        var perks = (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
            .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(builder)!;
        return perks[type];
    }

    private static IEnumerable<(PerkType Type, PerkDetail Detail)> AllPerks()
    {
        foreach (var definitionType in typeof(IPerkListDefinition).Assembly
                     .GetTypes()
                     .Where(x => !x.IsAbstract && typeof(IPerkListDefinition).IsAssignableFrom(x))
                     .OrderBy(x => x.FullName))
        {
            var definition = Activator.CreateInstance(definitionType)!;
            foreach (var method in definitionType
                         .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                         .Where(x => x.ReturnType == typeof(void) && x.GetParameters().Length == 0 && !x.Name.Contains('<'))
                         .OrderBy(x => x.MetadataToken))
            {
                method.Invoke(definition, null);
            }

            var builder = definitionType.GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(definition)!;
            var perks = (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
                .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(builder)!;
            foreach (var entry in perks)
                yield return (entry.Key, entry.Value);
        }
    }

    private static string ReadSource(params string[] pathParts)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        if (directory == null)
            throw new DirectoryNotFoundException("Could not locate SWLOR.Game.Server.sln from the test directory.");

        return File.ReadAllText(Path.Combine(new[] { directory.FullName }.Concat(pathParts).ToArray()));
    }

    private static string ExtractMethod(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        start.Should().BeGreaterThan(-1, $"'{signature}' must exist");
        var depth = 0;
        for (var index = source.IndexOf('{', start); index < source.Length; index++)
        {
            if (source[index] == '{') depth++;
            else if (source[index] == '}' && --depth == 0)
                return source[start..(index + 1)];
        }

        throw new InvalidOperationException($"Unbalanced braces after '{signature}'.");
    }
}
