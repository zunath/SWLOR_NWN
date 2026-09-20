using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Tests.Perks;

/// <summary>
/// Only the purchased rank's stat bonuses apply, so a rider introduced at one rank has to be
/// restated by every rank above it. Omitting it turns the next rank into a partial downgrade that
/// the player pays SP for.
/// </summary>
public class PerkRankProgressionTests
{
    [Test]
    public void NoPerkRank_SilentlyDropsABonusGrantedByTheRankBelowIt()
    {
        var regressions = new List<string>();

        foreach (var (type, detail) in AllPerks())
        {
            if (!detail.IsActive || detail.PerkLevels.Count < 2)
                continue;

            var ranks = detail.PerkLevels.OrderBy(x => x.Key).ToArray();
            for (var index = 1; index < ranks.Length; index++)
            {
                var lower = ranks[index - 1];
                var higher = ranks[index];

                foreach (var stat in lower.Value.StatBonuses.Select(x => x.Stat).Distinct())
                {
                    // Equipment-conditional bonuses evaluate against a live creature and cannot be
                    // resolved outside the engine, so they are out of scope for this corpus check.
                    var before = Total(lower.Value, stat);
                    var after = Total(higher.Value, stat);
                    if (before == null || after == null)
                        continue;

                    if (before > 0 && after <= 0)
                    {
                        regressions.Add(
                            $"{detail.Category}/{type} ({detail.Name}): {stat} is {before} at rank " +
                            $"{lower.Key} but absent at rank {higher.Key}");
                    }
                }
            }
        }

        regressions.Should().BeEmpty(
            "buying the next rank of a perk must never remove a benefit the previous rank granted:" +
            Environment.NewLine + string.Join(Environment.NewLine, regressions));
    }

    private static int? Total(PerkLevel level, StatType stat)
    {
        var total = 0;
        foreach (var bonus in level.StatBonuses.Where(x => x.Stat == stat))
        {
            int value;
            try
            {
                value = bonus.Calculate(0);
            }
            catch
            {
                return null;
            }

            total = Stat.AggregateStatAdjustment(stat, total, value);
        }

        return total;
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
}
