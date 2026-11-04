using System.Globalization;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Guards every beast definition against stat outliers: a beast whose per-level stats stray from the
/// standard curve of its role and tier (Razorhide Hound and Tuk'ata shipped with 230 HP at level 50
/// against a Damage-role standard of 530, and their mutations inherited the same mistake).
///
/// How the standard is defined:
///
/// Tier comes from the mutation graph. A tamable beast is Base. An incubation beast that a Base beast
/// mutates into is First (first mutation). One reached from a First beast is Second (second mutation).
/// An incubation beast with no mutations of its own and two or more parents is a shared DeadEnd (the
/// role-matched bad outcome). An incubation beast nothing mutates into is Unparented and may follow any
/// incubation tier. Only these parent and child links are read from the mutation lists.
///
/// The Base standard per role is the tamable beasts' consensus. The Bible "Beast Lookups" sheet
/// anchors it: <see cref="BaseTierMatchesTheBibleBeastLookups"/> proves FP and STM at every level and
/// the level 50 attack, accuracy, evasion, defense and resistance maxima come from that sheet.
/// First, Second and DeadEnd are expressed against the Base standard with the offsets the data uses
/// and BeastMutationExpansionPlan.md documents: Second is 10 to 15 percent above First, DeadEnd is
/// 20 to 30 percent below First, and First sits within 1 of Base.
///
/// HP and damage per second have no single curve: each beast is assigned its own rung on a ladder
/// (level 50 Damage tier 1 spans 350 to 850 HP in steps of 50), and damage and attack delay trade off
/// against each other. Those stats are therefore checked against the envelope of the role's tamable
/// beasts, with tolerances wide enough to hold today's whole spread and narrow enough to catch the
/// Razorhide Hound mistake (230 HP is 0.43 of the lowest Damage tamable; the lowest legitimate
/// mutation sits at 0.61 of its role's lowest tamable).
/// </summary>
public class BeastStatOutlierGuardTests
{
    private const int MaxLevel = 50;

    // Whole-number stats are rounded, so a derived window is widened by one point.
    private const double RoundingSlack = 1;

    // Observed: a First beast's maxima are 0 or 1 above the Base value at every level and stat.
    private const double FirstAboveBase = 1;

    // BeastMutationExpansionPlan.md: Second is 10 to 15 percent above the First baseline, DeadEnd is 20 to 30
    // percent below it. The data lands on 1.125 (HP 1.12 to 1.14) and 0.75.
    private const double SecondMinFactor = 1.10;
    private const double SecondMaxFactor = 1.15;
    private const double DeadEndMinFactor = 0.70;
    private const double DeadEndMaxFactor = 0.80;

    // Centre of the tier offsets, used to scale the HP and damage-per-second envelopes.
    private const double SecondCentre = 1.125;
    private const double DeadEndCentre = 0.75;

    // HP of a First, Second or DeadEnd beast, relative to the lowest and highest tamable HP of its role at the
    // same level (after dividing out the tier offset). Observed range at levels 10 to 50: 0.61 to 1.60. The
    // ceiling is only applied from level 10 because incubation beasts start with 100 to 180 HP at level 1
    // (tamable beasts start with 30 to 70), which only evens out as they level.
    private const double HpFloorOfBaseMinimum = 0.55;
    private const double HpCeilingOfBaseMaximum = 1.75;
    private const int HpCeilingFromLevel = 10;

    // HP of a tamable beast relative to the median tamable beast of its role. Observed range: 0.71 to 1.45.
    private const double BaseHpFloorOfMedian = 0.60;
    private const double BaseHpCeilingOfMedian = 1.60;

    // Damage per second (damage divided by the attack delay in seconds). Observed after dividing out the tier
    // offset: 0.84 of the role's lowest tamable to 1.17 of its highest.
    private const double DpsFloor = 0.75;
    private const double DpsCeiling = 1.25;

    // Every beast level uses an attack delay from 2.0 to 2.4 seconds.
    private const ItemPropertyAttackDelay SlowestDelayAllowed = ItemPropertyAttackDelay.Delay240;
    private const ItemPropertyAttackDelay FastestDelayAllowed = ItemPropertyAttackDelay.Delay200;

    // A dead end is a beast with no mutations and at least this many parents. Good second-stage beasts have one.
    private const int DeadEndMinimumParents = 2;

    /// <summary>
    /// Outliers the owner has not decided on yet. Key is "BeastType|Stat". Value says why it is accepted.
    /// Every entry must still be an outlier, so fixing the stats forces the entry to be deleted.
    /// </summary>
    private static readonly Dictionary<string, string> KnownOutliers = new();

    [Test]
    public void EveryBeastFollowsTheStandardOfItsRoleAndTier()
    {
        var (beasts, tamable) = LoadBeasts();
        var tiers = ClassifyTiers(beasts, tamable);

        var outliers = FindOutliers(beasts, tamable, tiers);
        var unexpected = outliers
            .Where(outlier => !KnownOutliers.ContainsKey(outlier.Key))
            .Select(outlier => outlier.Message)
            .ToList();
        var stale = KnownOutliers.Keys
            .Where(key => outliers.All(outlier => outlier.Key != key))
            .ToList();

        unexpected.Should().BeEmpty(
            "each beast must follow the standard of its role and tier; fix the stats or, with the owner's approval, " +
            "list the beast and stat in KnownOutliers. All outliers:{0}{1}",
            Environment.NewLine,
            string.Join(Environment.NewLine, unexpected));
        stale.Should().BeEmpty("KnownOutliers entries that are no longer outliers must be deleted");
    }

    [Test]
    public void EveryRoleHasEnoughTamableBeastsToDefineItsStandard()
    {
        var (beasts, tamable) = LoadBeasts();

        foreach (var role in Enum.GetValues<BeastRoleType>().Where(role => role != BeastRoleType.Invalid))
        {
            tamable.Count(type => beasts[type].Role == role)
                .Should().BeGreaterThanOrEqualTo(3, $"the {role} role standard is the consensus of its tamable beasts");
        }

        foreach (var (type, detail) in beasts)
        {
            detail.Levels.Keys.Should().BeEquivalentTo(Enumerable.Range(1, MaxLevel), $"{type} needs every level");
        }
    }

    [Test]
    public void TheMutationLadderYieldsEveryTier()
    {
        var (beasts, tamable) = LoadBeasts();
        var tiers = ClassifyTiers(beasts, tamable);

        tiers.Count.Should().Be(beasts.Count);
        tiers.Where(pair => tamable.Contains(pair.Key)).Select(pair => pair.Value).Distinct()
            .Should().BeEquivalentTo(new[] { BeastStandardTier.Base });
        tiers.Where(pair => !tamable.Contains(pair.Key)).Select(pair => pair.Value).Distinct()
            .Should().NotContain(BeastStandardTier.Base);
        tiers.Values.Should().Contain(new[]
        {
            BeastStandardTier.First,
            BeastStandardTier.Second,
            BeastStandardTier.DeadEnd,
        }, "the mutation ladder has all three incubation tiers");
    }

    [Test]
    public void BaseTierMatchesTheBibleBeastLookups()
    {
        var (beasts, tamable) = LoadBeasts();
        var lookups = BibleSheetReader.ReadRows("Beast Lookups");
        var failures = new List<string>();

        CheckLinearTable(lookups, "Start FP", "FP", level => level.FP, beasts, tamable, failures);
        CheckLinearTable(lookups, "Start STM", "STM", level => level.STM, beasts, tamable, failures);
        CheckMaximumsAtLevel50(lookups, beasts, tamable, failures);

        failures.Should().BeEmpty("tamable beasts are built from the Beast Lookups role and grade tables");
    }

    [Test]
    public void Guard_FlagsAHpCurveFarBelowTheRoleStandard()
    {
        var (beasts, tamable) = LoadBeasts();

        // The shipped Razorhide Hound bug: 4 HP gained per level, 230 HP at level 50.
        foreach (var (level, detail) in beasts[BeastType.RazorhideHound].Levels)
        {
            detail.HP = 30 + (level - 1) * 4;
        }

        var outliers = FindOutliers(beasts, tamable, ClassifyTiers(beasts, tamable));

        outliers.Select(outlier => outlier.Key).Should().Contain("RazorhideHound|HP");
    }

    [Test]
    public void Guard_FlagsAMutationGivenTheWrongTierCurve()
    {
        var (beasts, tamable) = LoadBeasts();

        // A first mutation carrying the second-mutation curve for FP.
        foreach (var (level, detail) in beasts[BeastType.RazorhideHound].Levels)
        {
            detail.FP = beasts[BeastType.DirefangLupikar].Levels[level].FP;
        }

        var outliers = FindOutliers(beasts, tamable, ClassifyTiers(beasts, tamable));

        outliers.Select(outlier => outlier.Key).Should().Contain("RazorhideHound|FP");
    }

    [Test]
    public void Guard_FlagsAnAttackDelayOutsideTheBeastRange()
    {
        var (beasts, tamable) = LoadBeasts();
        beasts[BeastType.QionTiger].Levels[MaxLevel].AttackDelay = ItemPropertyAttackDelay.Delay300;

        var outliers = FindOutliers(beasts, tamable, ClassifyTiers(beasts, tamable));

        outliers.Select(outlier => outlier.Key).Should().Contain("QionTiger|Attack delay");
    }

    private static (Dictionary<BeastType, BeastDetail> Beasts, HashSet<BeastType> Tamable) LoadBeasts()
    {
        var beasts = new Dictionary<BeastType, BeastDetail>();
        var tamable = new HashSet<BeastType>();
        var definitionTypes = typeof(IBeastListDefinition).Assembly
            .GetTypes()
            .Where(type =>
                !type.IsAbstract &&
                !type.IsInterface &&
                typeof(IBeastListDefinition).IsAssignableFrom(type));

        foreach (var definitionType in definitionTypes)
        {
            var isTamable = definitionType.Namespace!.EndsWith(".TamableBeastDefinition", StringComparison.Ordinal);
            var definition = (IBeastListDefinition)Activator.CreateInstance(definitionType)!;
            foreach (var (beastType, detail) in definition.Build())
            {
                beasts.Add(beastType, detail);
                if (isTamable)
                    tamable.Add(beastType);
            }
        }

        return (beasts, tamable);
    }

    private static Dictionary<BeastType, BeastStandardTier> ClassifyTiers(
        Dictionary<BeastType, BeastDetail> beasts,
        HashSet<BeastType> tamable)
    {
        var parents = beasts.Keys.ToDictionary(type => type, _ => new List<BeastType>());
        foreach (var (parent, detail) in beasts)
        {
            foreach (var mutation in detail.PossibleMutations)
            {
                parents[mutation.Type].Add(parent);
            }
        }

        // Ladder depth: 0 for tamable beasts, otherwise one more than the shallowest parent. A beast with no
        // parents counts as depth 1 for its children.
        var depths = new Dictionary<BeastType, int>();
        int DepthOf(BeastType type, HashSet<BeastType> path)
        {
            if (depths.TryGetValue(type, out var known))
                return known;
            if (tamable.Contains(type))
                return depths[type] = 0;
            if (parents[type].Count == 0 || !path.Add(type))
                return 1;

            var depth = 1 + parents[type].Min(parent => DepthOf(parent, path));
            path.Remove(type);
            return depths[type] = depth;
        }

        var tiers = new Dictionary<BeastType, BeastStandardTier>();
        foreach (var (type, detail) in beasts)
        {
            if (tamable.Contains(type))
                tiers[type] = BeastStandardTier.Base;
            else if (parents[type].Count == 0)
                tiers[type] = BeastStandardTier.Unparented;
            else if (detail.PossibleMutations.Count == 0 && parents[type].Count >= DeadEndMinimumParents)
                tiers[type] = BeastStandardTier.DeadEnd;
            else
                tiers[type] = DepthOf(type, new HashSet<BeastType>()) <= 1
                    ? BeastStandardTier.First
                    : BeastStandardTier.Second;
        }

        return tiers;
    }

    private static List<(string Key, string Message)> FindOutliers(
        Dictionary<BeastType, BeastDetail> beasts,
        HashSet<BeastType> tamable,
        Dictionary<BeastType, BeastStandardTier> tiers)
    {
        var exactStats = BuildExactStatReaders(beasts);
        var tamableByRole = beasts
            .Where(pair => tamable.Contains(pair.Key))
            .GroupBy(pair => pair.Value.Role)
            .ToDictionary(group => group.Key, group => group.Select(pair => pair.Value).ToList());

        var violations = new Dictionary<(BeastType Beast, string Stat), List<string>>();
        void Record(BeastType type, string stat, int level, string message)
        {
            if (!violations.TryGetValue((type, stat), out var list))
                violations[(type, stat)] = list = new List<string>();
            list.Add($"L{level} {message}");
        }

        for (var level = 1; level <= MaxLevel; level++)
        {
            foreach (var (role, peers) in tamableByRole)
            {
                var peerLevels = peers.Select(peer => peer.Levels[level]).ToList();
                var baseValues = exactStats.ToDictionary(
                    stat => stat.Name,
                    stat => peerLevels.Select(stat.Read).GroupBy(value => value)
                        .OrderByDescending(group => group.Count()).ThenBy(group => group.Key).First().Key);
                var hp = peerLevels.Select(l => (double)l.HP).OrderBy(value => value).ToList();
                var dps = peerLevels.Select(DamagePerSecond).OrderBy(value => value).ToList();

                foreach (var (type, detail) in beasts.Where(pair => pair.Value.Role == role))
                {
                    var tier = tiers[type];
                    var beastLevel = detail.Levels[level];

                    foreach (var stat in exactStats)
                    {
                        var value = stat.Read(beastLevel);
                        if (!Accepts(tier, candidate => ExactWindow(candidate, baseValues[stat.Name]), value, out var expected))
                            Record(type, stat.Name, level, $"{value} (expected {expected})");
                    }

                    var hpValue = (double)beastLevel.HP;
                    if (!Accepts(tier, candidate => HpWindow(candidate, level, hp), hpValue, out var hpExpected))
                        Record(type, "HP", level, $"{beastLevel.HP} (expected {hpExpected})");

                    var dpsValue = DamagePerSecond(beastLevel);
                    if (!Accepts(tier, candidate => DpsWindow(candidate, dps), dpsValue, out var dpsExpected))
                        Record(type, "Damage per second", level,
                            $"{dpsValue.ToString("0.0", CultureInfo.InvariantCulture)} from damage {beastLevel.DMG} and delay {beastLevel.AttackDelay} (expected {dpsExpected})");

                    if (beastLevel.AttackDelay < FastestDelayAllowed || beastLevel.AttackDelay > SlowestDelayAllowed)
                        Record(type, "Attack delay", level,
                            $"{beastLevel.AttackDelay} (expected {FastestDelayAllowed} to {SlowestDelayAllowed})");
                }
            }
        }

        return violations
            .OrderBy(pair => pair.Key.Beast.ToString(), StringComparer.Ordinal)
            .ThenBy(pair => pair.Key.Stat, StringComparer.Ordinal)
            .Select(pair => (
                Key: $"{pair.Key.Beast}|{pair.Key.Stat}",
                Message: $"{beasts[pair.Key.Beast].Name} ({pair.Key.Beast}, {tiers[pair.Key.Beast]} {beasts[pair.Key.Beast].Role}) " +
                         $"{pair.Key.Stat} is off the standard on {pair.Value.Count} levels, e.g. {pair.Value[^1]}"))
            .ToList();
    }

    private static List<(string Name, Func<BeastLevel, int> Read)> BuildExactStatReaders(
        Dictionary<BeastType, BeastDetail> beasts)
    {
        var sample = beasts.Values.First().Levels[1];
        var readers = new List<(string, Func<BeastLevel, int>)>
        {
            ("FP", level => level.FP),
            ("STM", level => level.STM),
            ("Max attack bonus", level => level.MaxAttackBonus),
            ("Max accuracy bonus", level => level.MaxAccuracyBonus),
            ("Max evasion bonus", level => level.MaxEvasionBonus),
        };

        foreach (var damageType in sample.MaxDefenseBonuses.Keys)
        {
            var captured = damageType;
            readers.Add(($"Max {captured} defense", level => level.MaxDefenseBonuses[captured]));
        }

        foreach (var resistanceType in sample.MaxResistanceBonuses.Keys)
        {
            var captured = resistanceType;
            readers.Add(($"Max {captured} resistance", level => level.MaxResistanceBonuses[captured]));
        }

        return readers;
    }

    /// <summary>True when the value fits the window of the tier. An unparented beast may match any incubation tier.</summary>
    private static bool Accepts(
        BeastStandardTier tier,
        Func<BeastStandardTier, (double Min, double Max)> window,
        double value,
        out string expected)
    {
        var candidates = tier == BeastStandardTier.Unparented
            ? new[] { BeastStandardTier.First, BeastStandardTier.Second, BeastStandardTier.DeadEnd }
            : new[] { tier };

        var descriptions = new List<string>();
        foreach (var candidate in candidates)
        {
            var (min, max) = window(candidate);
            if (value >= min - 1e-9 && value <= max + 1e-9)
            {
                expected = string.Empty;
                return true;
            }

            descriptions.Add(max >= double.MaxValue
                ? $">= {Format(min)}"
                : min == max ? Format(min) : $"{Format(min)} to {Format(max)}");
        }

        expected = string.Join(" or ", descriptions);
        return false;
    }

    private static string Format(double value)
    {
        return value.ToString("0.#", CultureInfo.InvariantCulture);
    }

    private static (double Min, double Max) ExactWindow(BeastStandardTier tier, int baseValue)
    {
        return tier switch
        {
            BeastStandardTier.Base => (baseValue, baseValue),
            BeastStandardTier.First => (baseValue, baseValue + FirstAboveBase),
            BeastStandardTier.Second => (
                baseValue * SecondMinFactor - RoundingSlack,
                (baseValue + FirstAboveBase) * SecondMaxFactor + RoundingSlack),
            BeastStandardTier.DeadEnd => (
                baseValue * DeadEndMinFactor - RoundingSlack,
                (baseValue + FirstAboveBase) * DeadEndMaxFactor + RoundingSlack),
            _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, null),
        };
    }

    private static (double Min, double Max) HpWindow(BeastStandardTier tier, int level, List<double> baseHp)
    {
        if (tier == BeastStandardTier.Base)
        {
            var median = Median(baseHp);
            return (median * BaseHpFloorOfMedian, median * BaseHpCeilingOfMedian);
        }

        var scale = TierScale(tier);
        var ceiling = level >= HpCeilingFromLevel
            ? baseHp.Max() * scale * HpCeilingOfBaseMaximum
            : double.MaxValue;
        return (baseHp.Min() * scale * HpFloorOfBaseMinimum, ceiling);
    }

    private static (double Min, double Max) DpsWindow(BeastStandardTier tier, List<double> baseDps)
    {
        if (tier == BeastStandardTier.Base)
        {
            var median = Median(baseDps);
            return (median * DpsFloor, median * DpsCeiling);
        }

        var scale = TierScale(tier);
        return (baseDps.Min() * scale * DpsFloor, baseDps.Max() * scale * DpsCeiling);
    }

    private static double TierScale(BeastStandardTier tier)
    {
        return tier switch
        {
            BeastStandardTier.Second => SecondCentre,
            BeastStandardTier.DeadEnd => DeadEndCentre,
            _ => 1.0,
        };
    }

    private static double DamagePerSecond(BeastLevel level)
    {
        return level.DMG / ((int)level.AttackDelay / 10.0);
    }

    private static double Median(List<double> sorted)
    {
        var middle = sorted.Count / 2;
        return sorted.Count % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2;
    }

    private static void CheckLinearTable(
        SortedDictionary<int, Dictionary<string, string>> lookups,
        string startHeader,
        string statName,
        Func<BeastLevel, int> read,
        Dictionary<BeastType, BeastDetail> beasts,
        HashSet<BeastType> tamable,
        List<string> failures)
    {
        var header = lookups.First(row => row.Value.TryGetValue("B", out var text) && text.Trim() == startHeader).Key;
        var table = new Dictionary<BeastRoleType, (double Start, double Increase)>();
        for (var row = header + 1; lookups.TryGetValue(row, out var cells) && HasText(cells, "A"); row++)
        {
            table[Enum.Parse<BeastRoleType>(cells["A"].Trim())] = (Number(cells["B"]), Number(cells["C"]));
        }

        foreach (var type in tamable)
        {
            var detail = beasts[type];
            var (start, increase) = table[detail.Role];
            for (var level = 1; level <= MaxLevel; level++)
            {
                var expected = (int)Math.Floor(start + increase * (level - 1) + 0.5);
                var actual = read(detail.Levels[level]);
                if (actual != expected)
                    failures.Add($"{detail.Name} ({detail.Role}) {statName} L{level}: Bible {expected}, game {actual}");
            }
        }
    }

    private static void CheckMaximumsAtLevel50(
        SortedDictionary<int, Dictionary<string, string>> lookups,
        Dictionary<BeastType, BeastDetail> beasts,
        HashSet<BeastType> tamable,
        List<string> failures)
    {
        // Role -> grade per stat.
        var roleHeader = lookups.First(row => row.Value.TryGetValue("A", out var text) && text.Trim() == "Beast Role");
        var columns = roleHeader.Value
            .Where(cell => cell.Key != "A" && !string.IsNullOrWhiteSpace(cell.Value))
            .ToDictionary(cell => cell.Key, cell => cell.Value.Replace('\n', ' ').Trim());
        var grades = new Dictionary<BeastRoleType, Dictionary<string, string>>();
        for (var row = roleHeader.Key + 1; lookups.TryGetValue(row, out var cells) && HasText(cells, "A"); row++)
        {
            grades[Enum.Parse<BeastRoleType>(cells["A"].Trim())] =
                columns.ToDictionary(column => column.Value, column => cells[column.Key].Trim());
        }

        // Grade -> level 50 maximum: the table whose Grade A row holds raw maxima (the other Grade tables
        // hold percentages and formulas).
        var gradeRow = lookups
            .Where(row => row.Value.TryGetValue("A", out var text) && text.Trim() == "Grade")
            .Select(row => row.Key)
            .First(row => lookups.TryGetValue(row + 1, out var first) && Number(first["B"]) > 1);
        var maximums = new Dictionary<string, Dictionary<string, double>>();
        for (var row = gradeRow + 1; lookups.TryGetValue(row, out var cells) && HasText(cells, "A"); row++)
        {
            maximums[cells["A"].Trim()] = columns.ToDictionary(column => column.Value, column => Number(cells[column.Key]));
        }

        // Bible column -> game stat. Fortitude, Will and Reflex are the Trauma, Mind and Mobility resistances.
        var readers = new Dictionary<string, Func<BeastLevel, int>>
        {
            ["Attack Bonus"] = level => level.MaxAttackBonus,
            ["Accuracy Bonus"] = level => level.MaxAccuracyBonus,
            ["Evasion Bonus"] = level => level.MaxEvasionBonus,
            ["Physical DEF Bonus"] = level => level.MaxDefenseBonuses[CombatDamageType.Physical],
            ["Force DEF Bonus"] = level => level.MaxDefenseBonuses[CombatDamageType.Force],
            ["Fire DEF"] = level => level.MaxResistanceBonuses[ResistanceType.Fire],
            ["Ice DEF"] = level => level.MaxResistanceBonuses[ResistanceType.Ice],
            ["Poison DEF"] = level => level.MaxResistanceBonuses[ResistanceType.Poison],
            ["Electrical DEF"] = level => level.MaxResistanceBonuses[ResistanceType.Electrical],
            ["Fortitude Bonus"] = level => level.MaxResistanceBonuses[ResistanceType.Trauma],
            ["Will Bonus"] = level => level.MaxResistanceBonuses[ResistanceType.Mind],
            ["Reflex Bonus"] = level => level.MaxResistanceBonuses[ResistanceType.Mobility],
        };

        columns.Values.Should().BeEquivalentTo(readers.Keys, "every Beast Lookups grade column maps to a beast stat");

        foreach (var type in tamable)
        {
            var detail = beasts[type];
            foreach (var (column, read) in readers)
            {
                var expected = (int)Math.Floor(maximums[grades[detail.Role][column]][column] + 0.5);
                var actual = read(detail.Levels[MaxLevel]);
                if (actual != expected)
                    failures.Add($"{detail.Name} ({detail.Role}) {column} L{MaxLevel}: Bible grade {grades[detail.Role][column]} = {expected}, game {actual}");
            }
        }
    }

    private static bool HasText(Dictionary<string, string> cells, string column)
    {
        return cells.TryGetValue(column, out var text) && !string.IsNullOrWhiteSpace(text);
    }

    private static double Number(string text)
    {
        return double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}
