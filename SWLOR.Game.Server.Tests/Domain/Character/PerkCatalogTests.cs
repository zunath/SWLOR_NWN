using System.Globalization;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>
/// The perk catalog as the server builds it, pinned so a change to how the perk code is organized cannot change what a perk
/// grants. A perk type is defined once; and a fixture written on the current code lists, for every perk, the stat bonuses
/// each level contributes (<c>GetStatSources</c> and <c>GetStatBonus</c>), the force affinity reads, and the granted and current
/// active ability feats of every level, so the same reads after a change are compared with it byte for byte.
/// </summary>
[TestFixture]
[NonParallelizable]
public class PerkCatalogTests
{
    private const string FixturePath = "SWLOR.Game.Server.Tests/Fixtures/CharacterTranscripts/PerkCatalog.txt";

    private static readonly int[] Affinities = { -10, -5, -1, 0, 1, 5, 10 };

    [Test]
    public void NoPerkTypeIsDefinedByTwoPerkLists()
    {
        using var bench = new PerkBench();
        using var facades = new FacadeContainer();
        using var installed = facades.Install();

        var definitions = bench.Catalog.DefaultPerkListDefinitionTypes()
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .SelectMany(type => ((IPerkListDefinition)Activator.CreateInstance(type)!).BuildPerks().Keys.Select(perk => (Perk: perk, List: type.Name)))
            .ToArray();

        definitions.Should().NotBeEmpty();
        var duplicated = definitions
            .GroupBy(definition => definition.Perk)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key}: {string.Join(", ", group.Select(definition => definition.List))}")
            .ToArray();
        duplicated.Should().BeEmpty("a perk type built by two lists would be cached twice, and the table keeps the last");
    }

    [Test]
    public void EveryPerkOfTheServerCatalog_ReadsAsItDidWhenTheFixtureWasWritten()
    {
        using var bench = new PerkBench { UseServerPerkLists = true };
        using var facades = new FacadeContainer();
        using var installed = facades.Install();
        // The ability definitions of the server, built as the catalog builds them (the feats a perk grants are looked up there).
        foreach (var type in GameAssemblies.GetAssemblies().SelectMany(assembly => assembly.GetTypes())
                     .Where(type => typeof(IAbilityListDefinition).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                     .OrderBy(type => type.FullName, StringComparer.Ordinal))
        {
            foreach (var (feat, detail) in ((IAbilityListDefinition)Activator.CreateInstance(type)!).BuildAbilities())
                bench.Abilities[feat] = detail;
        }

        bench.Abilities.Should().NotBeEmpty();

        bench.CacheData();
        bench.Feats.CachePerkFeatData();

        // The fixture's numbers use a dot for decimals, so the text is built in the invariant culture whatever the machine's is.
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        try
        {
            var text = new StringBuilder();
            var perks = bench.Catalog.GetAllPerks().OrderBy(pair => (int)pair.Key).ToArray();
            text.Append("perks ").Append(perks.Length).Append('\n');
            foreach (var (perkType, detail) in perks)
            {
                text.Append("perk ").Append((int)perkType).Append(' ').Append(perkType).Append(" levels=")
                    .Append(string.Join(",", detail.PerkLevels.Keys)).Append(" active=").Append(detail.IsActive)
                    .Append(" group=").Append(detail.GroupType).Append(" affinity=")
                    .Append(detail.ForceAffinityType?.ToString() ?? "none").Append('\n');
                AppendFeats(text, bench, perkType, detail);
                AppendStatBonuses(text, bench, perkType, detail);
                AppendAffinity(text, bench, perkType, detail);
            }

            CombatTranscriptBench.AssertGolden(FixturePath, text.ToString());
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    private static void AppendFeats(StringBuilder text, PerkBench bench, PerkType perkType, PerkDetail detail)
    {
        text.Append("  recast=").Append(bench.Feats.GetActiveAbilityRecastGroup(perkType))
            .Append(" allActive=[").Append(string.Join(",", bench.Feats.GetAllActiveAbilityFeats(perkType).Select(feat => (int)feat))).Append("]\n");
        foreach (var level in detail.PerkLevels.Keys)
        {
            text.Append("  level ").Append(level)
                .Append(" granted=[").Append(string.Join(",", bench.Feats.GetGrantedFeatsForPerkLevel(perkType, level).Select(feat => (int)feat)))
                .Append("] current=[").Append(string.Join(",", bench.Feats.GetCurrentActiveAbilityFeats(perkType, level).Select(feat => (int)feat)))
                .Append("]\n");
        }
    }

    private static void AppendStatBonuses(StringBuilder text, PerkBench bench, PerkType perkType, PerkDetail detail)
    {
        var stats = detail.StatBonuses.Select(bonus => bonus.Stat)
            .Concat(detail.PerkLevels.Values.SelectMany(level => level.StatBonuses.Select(bonus => bonus.Stat)))
            .Distinct()
            .OrderBy(stat => (int)stat)
            .ToArray();
        foreach (var level in detail.PerkLevels.Keys)
        {
            bench.LocalInts[(PerkBench.Npc, $"PERK_LEVEL_{(int)perkType}")] = level;
            foreach (var stat in stats)
            {
                text.Append("  level ").Append(level).Append(" stat ").Append(stat);
                try
                {
                    var sources = bench.StatBonus.GetStatSources(PerkBench.Npc, stat);
                    text.Append(" bonus=").Append(bench.StatBonus.GetStatBonus(PerkBench.Npc, stat));
                    foreach (var source in sources)
                    {
                        text.Append(" source=").Append(source.Key).Append('=').Append(source[stat]);
                    }
                }
                catch (Exception exception)
                {
                    text.Append(" throws=").Append(exception.GetType().Name);
                }

                text.Append('\n');
            }
        }

        bench.LocalInts.Remove((PerkBench.Npc, $"PERK_LEVEL_{(int)perkType}"));
    }

    private static void AppendAffinity(StringBuilder text, PerkBench bench, PerkType perkType, PerkDetail detail)
    {
        if (detail.ForceAffinityType == null)
        {
            text.Append("  affinity none: multiplier=").Append(bench.Affinity.GetForceAffinityMagnitudeMultiplier(PerkBench.Player, perkType)).Append('\n');
            return;
        }

        foreach (var affinity in Affinities)
        {
            bench.StatAdjustments[(PerkBench.Player, StatType.ForceAffinity)] = affinity;
            var found = bench.Affinity.TryGetForceSideAffinity(PerkBench.Player, perkType, out var side);
            text.Append("  affinity ").Append(affinity).Append(": side=").Append(found ? side.ToString() : "none")
                .Append(" multiplier=").Append(bench.Affinity.GetForceAffinityMagnitudeMultiplier(PerkBench.Player, perkType))
                .Append(" hit=").Append(bench.Affinity.GetForceAffinityHitChanceAdjustment(PerkBench.Player, perkType))
                .Append(" restorative=").Append(bench.Affinity.GetForceAffinityRestorativeMagnitudeMultiplier(PerkBench.Player, perkType))
                .Append(" magnitude=").Append(bench.Affinity.ApplyForceAffinityMagnitude(PerkBench.Player, perkType, 100))
                .Append(" restoredMagnitude=").Append(bench.Affinity.ApplyForceAffinityRestorativeMagnitude(PerkBench.Player, perkType, 100))
                .Append('\n');
        }

        bench.StatAdjustments.Remove((PerkBench.Player, StatType.ForceAffinity));
    }
}
