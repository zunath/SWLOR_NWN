using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Guards the identity-stat rule.
///
/// Most StatTypes carry a magnitude, and two sources granting the same one should add up. A few
/// carry an identifier instead - a cast enum naming which skill, category, or effect the behavior
/// applies to. Summing those is meaningless: two perks each declaring "(int)SkillType.Spear" (40)
/// produce 80, which is not a defined SkillType, so the dependent behavior silently switches off.
/// Worse, some sums land on a valid enum member and retarget the behavior onto something else -
/// Control(2) + Control(2) = 4 = Bleeding.
///
/// The fix is declarative: identity stats are marked with isIdentity on their StatTypeAttribute and
/// the stat pipeline combines them with Max instead of Sum. This test exists so a newly added
/// identity stat cannot silently miss the annotation - it scans the definition sources for stats
/// written as a cast enum and requires each one to be marked.
/// </summary>
public class IdentityStatTypeTests
{
    private static readonly Regex CastEnumStatWrite = new(
        @"StatType\.(?<stat>[A-Za-z0-9_]+)\s*(?:,|\]\s*=)\s*\(int\)(?<enum>[A-Za-z0-9_]+)\.",
        RegexOptions.Compiled);

    /// <summary>
    /// Enum types that name a target rather than measuring one. A StatType written from one of
    /// these is an identity stat.
    /// </summary>
    private static readonly HashSet<string> IdentityEnums = new(StringComparer.Ordinal)
    {
        "SkillType",
        "StatusEffectCategory",
        "PerkType",
        "PerkCategoryType",
        "BuffStatusEffectType",
        "CombatDamageType",
        "ResistanceType",
        "RecastGroup",
    };


    [Test]
    public void EveryStatWrittenAsACastEnumIsMarkedAsIdentity()
    {
        var unmarked = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var (statName, sourceFile) in FindCastEnumStatWrites())
        {
            if (!Enum.TryParse<StatType>(statName, out var statType))
                continue;

            if (!Stat.IsIdentityStat(statType))
            {
                unmarked.Add($"{statName} (written as a cast enum in {sourceFile})");
            }
        }

        unmarked.Should().BeEmpty(
            "every StatType assigned a cast enum value must declare isIdentity: true on its " +
            "StatTypeAttribute, or two sources granting it will sum into a different identifier");
    }

    [Test]
    public void IdentityStatsResolveToTheHighestContributorInsteadOfSumming()
    {
        var identityStat = StatType.AbilityUsedEvasionPercentAdjustmentSkillType;
        var magnitudeStat = StatType.AbilityUsedEvasionPercentAdjustment;

        Stat.IsIdentityStat(identityStat).Should().BeTrue();
        Stat.IsIdentityStat(magnitudeStat).Should().BeFalse();

        // Two Spear perks both declaring SkillType.Spear must still resolve to Spear, not 80.
        var spear = (int)SkillType.Spear;
        Stat.CombineStatAdjustment(identityStat, spear, spear).Should().Be(spear);

        // Magnitudes are unaffected and continue to stack.
        Stat.CombineStatAdjustment(magnitudeStat, 10, 3).Should().Be(13);
    }

    /// <summary>
    /// ForceAffinity is written from a cast ForceAffinityType and so looks like an identity stat,
    /// but it is read back through a clamped sum: light and dark affinity are meant to accumulate
    /// across perks. It is a signed magnitude that happens to be built out of an enum, and marking
    /// it as identity would stop affinity from progressing.
    /// </summary>
    [Test]
    public void ForceAffinityAccumulatesAndIsNotAnIdentityStat()
    {
        Stat.IsIdentityStat(StatType.ForceAffinity).Should().BeFalse();
        Stat.CombineStatAdjustment(StatType.ForceAffinity, 3, 4).Should().Be(7);
    }

    private static IEnumerable<(string StatName, string SourceFile)> FindCastEnumStatWrites()
    {
        var root = FindRepositoryRoot();
        var searchRoots = new[]
        {
            Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "PerkDefinition"),
            Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "StatusEffectDefinition"),
            Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "AbilityDefinition"),
        };

        foreach (var searchRoot in searchRoots.Where(Directory.Exists))
        {
            foreach (var file in Directory.EnumerateFiles(searchRoot, "*.cs", SearchOption.AllDirectories))
            {
                var contents = File.ReadAllText(file);
                foreach (Match match in CastEnumStatWrite.Matches(contents))
                {
                    if (IdentityEnums.Contains(match.Groups["enum"].Value))
                    {
                        yield return (match.Groups["stat"].Value, Path.GetFileName(file));
                    }
                }
            }
        }
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "SWLOR_Haks")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
