using NUnit.Framework;
using SWLOR.Game.Server.CharacterizationTests.Definitions;
using SWLOR.Game.Server.CharacterizationTests.Support;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.CharacterizationTests.Service;

/// <summary>
/// Locks how every <see cref="StatType"/> is classified (category, aggregation, deflection source) and the functions that
/// read the classification: <see cref="Stat.AggregateStatAdjustment"/> merges adjustments by it, and
/// <see cref="Stat.IsBeneficialStatAdjustment"/> and <see cref="Stat.GetGrantedDeflectionStatType"/> look it up.
/// </summary>
public class StatTypeMetadataCharacterizationTests
{
    private const StatType UndefinedStat = (StatType)99999;

    [Test]
    public void StatTypeMetadata()
    {
        var stats = Enum.GetValues<StatType>().OrderBy(stat => DefinitionCell.Id(stat)).ToArray();

        var table = new SnapshotTable(
            "Stat.GetStatTypeCategory / GetStatTypeAggregation / GetStatTypeDeflectionSource over every StatType",
            "How each stat is classified: whether an untyped effect granting it counts as beneficial, how several adjustments of it combine, " +
            "and which deflection kind (if any) it belongs to.",
            $"All {stats.Length} StatType members, sorted by id.",
            new[] { "stat", "statName" },
            new[] { "category", "aggregation", "deflectionSource" })
        {
            AllowConstantOutputs = true
        };

        foreach (var stat in stats)
        {
            table.Add(
                DefinitionCell.Id(stat),
                stat,
                Stat.GetStatTypeCategory(stat),
                Stat.GetStatTypeAggregation(stat),
                Stat.GetStatTypeDeflectionSource(stat));
        }

        Snapshot.Verify("Stat", "StatTypeMetadata", table);
    }

    [Test]
    public void UndefinedStatTypeFallsBackToTheDefaults()
    {
        var table = new SnapshotTable(
            "Stat.GetStatTypeCategory / GetStatTypeAggregation / GetStatTypeDeflectionSource for a value that is not a StatType member",
            "The classification a stat id with no metadata gets.",
            "One undefined stat id (99999).",
            new[] { "stat" },
            new[] { "category", "aggregation", "deflectionSource", "grantedDeflectionStat" })
        {
            AllowConstantOutputs = true
        };

        table.Add(
            (int)UndefinedStat,
            Stat.GetStatTypeCategory(UndefinedStat),
            Stat.GetStatTypeAggregation(UndefinedStat),
            Stat.GetStatTypeDeflectionSource(UndefinedStat),
            Stat.GetGrantedDeflectionStatType(UndefinedStat));

        Snapshot.Verify("Stat", "UndefinedStatTypeDefaults", table);
    }

    [Test]
    public void GetGrantedDeflectionStatType()
    {
        var deflectionStats = Enum.GetValues<StatType>()
            .Where(stat => Stat.GetStatTypeDeflectionSource(stat) != DeflectionSource.None)
            .OrderBy(stat => DefinitionCell.Id(stat))
            .ToList();
        var controls = new[] { StatType.Invalid, StatType.AttackPercentAdjustment, StatType.Attack };

        var table = new SnapshotTable(
            "Stat.GetGrantedDeflectionStatType(StatType sourceStatType)",
            "The deflection stat a stat feeds: melee, ranged or shield deflection by its deflection source, otherwise Invalid.",
            $"All {deflectionStats.Count} stats that declare a deflection source, sorted by id, then three stats that do not.",
            new[] { "stat", "statName" },
            new[] { "deflectionSource", "grantedStat" })
        {
            AllowConstantOutputs = true
        };

        foreach (var stat in deflectionStats.Concat(controls.Where(control => !deflectionStats.Contains(control))))
        {
            table.Add(
                DefinitionCell.Id(stat),
                stat,
                Stat.GetStatTypeDeflectionSource(stat),
                Stat.GetGrantedDeflectionStatType(stat));
        }

        Snapshot.Verify("Stat", "GetGrantedDeflectionStatType", table);
    }

    [Test]
    public void AggregateStatAdjustment()
    {
        var stats = new[]
        {
            StatType.AttackPercentAdjustment,
            StatType.OpeningAutoAttackIdleSeconds,
            StatType.StatusAppliedRequiredCategory,
            StatType.Invalid,
            UndefinedStat
        };

        var table = new SnapshotTable(
            "Stat.AggregateStatAdjustment(StatType statType, int current, int adjustment)",
            "Combines two adjustments of one stat: added, taken as the larger, or bitwise OR-ed, according to the stat's aggregation.",
            "one additive stat, one maximum stat, one bitwise-OR stat, Invalid and an undefined id (both additive) x current {-8,-1,0,1,5,6,12} x adjustment {-8,-1,0,1,3,6,10}.",
            new[] { "stat", "statName", "aggregation", "current", "adjustment" },
            new[] { "combined" });

        foreach (var stat in stats)
        {
            foreach (var row in Grid.Cross(
                         new[] { -8, -1, 0, 1, 5, 6, 12 },
                         new[] { -8, -1, 0, 1, 3, 6, 10 }))
            {
                table.Add(
                    (int)stat,
                    Enum.IsDefined(stat) ? stat.ToString() : "(undefined)",
                    Stat.GetStatTypeAggregation(stat),
                    row[0],
                    row[1],
                    Stat.AggregateStatAdjustment(stat, row[0], row[1]));
            }
        }

        Snapshot.Verify("Stat", "AggregateStatAdjustment", table);
    }

    [Test]
    public void IsBeneficialStatAdjustment()
    {
        var stats = new[]
        {
            StatType.AttackPercentAdjustment,
            StatType.DamageTakenPercentAdjustment,
            StatType.PairedRangedMomentumRestoreCooldownSeconds,
            StatType.Invalid,
            UndefinedStat
        };

        var table = new SnapshotTable(
            "Stat.IsBeneficialStatAdjustment(StatType statType, int value)",
            "Whether an adjustment value helps its holder: positive for most stats, negative for cost and damage-taken stats, never for neutral ones and never at zero.",
            "one beneficial-when-positive stat, one beneficial-when-negative stat, one non-beneficial stat, Invalid and an undefined id x value {-100,-1,0,1,100}.",
            new[] { "stat", "statName", "category", "value" },
            new[] { "beneficial" });

        foreach (var stat in stats)
        {
            foreach (var value in new[] { -100, -1, 0, 1, 100 })
            {
                table.Add(
                    (int)stat,
                    Enum.IsDefined(stat) ? stat.ToString() : "(undefined)",
                    Stat.GetStatTypeCategory(stat),
                    value,
                    Stat.IsBeneficialStatAdjustment(stat, value));
            }
        }

        Snapshot.Verify("Stat", "IsBeneficialStatAdjustment", table);
    }
}
