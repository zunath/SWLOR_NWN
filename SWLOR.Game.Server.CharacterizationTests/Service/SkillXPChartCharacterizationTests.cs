using NUnit.Framework;
using SWLOR.Game.Server.CharacterizationTests.Support;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.CharacterizationTests.Service;

/// <summary>
/// Locks the skill XP chart and the XP lookups built on it: the XP needed per rank, the cumulative XP, the reverse lookup
/// from total XP to rank, the rank-difference XP table and the practice and Espionage XP rules. All of it is plain
/// data and arithmetic once <see cref="Skill.CacheXPChartData"/> has built the cumulative table.
/// </summary>
[NonParallelizable]
public class SkillXPChartCharacterizationTests
{
    [OneTimeSetUp]
    public void BuildChart()
    {
        // Pure: sums the chart into the cumulative table and records the highest rank difference. The same call runs at module load.
        Skill.CacheXPChartData();
    }

    [Test]
    public void GetRequiredXP_AndGetTotalRequiredXP()
    {
        var table = new SnapshotTable(
            "Skill.GetRequiredXP(int level) / Skill.GetTotalRequiredXP(int level)",
            "The XP needed to leave a rank, and the XP earned in total once the rank has been completed (the sum of the requirements up to and including it). " +
            "Ranks outside the chart throw, and the thrown text is recorded.",
            "Every rank 0..100 and the neighbours outside the chart (-2, -1, 101, 102).",
            new[] { "rank" },
            new[] { "requiredXP", "totalRequiredXP" });

        for (var level = -2; level <= 102; level++)
        {
            var captured = level;
            table.Add(level, Outcome.Value(() => Skill.GetRequiredXP(captured)), Outcome.Value(() => Skill.GetTotalRequiredXP(captured)));
        }

        Snapshot.Verify("Skill", "GetRequiredXP", table);
    }

    [Test]
    public void GetLevelByTotalXP()
    {
        var table = new SnapshotTable(
            "Skill.GetLevelByTotalXP(int totalXP)",
            "The rank a total XP amount falls in and the XP earned towards that rank. A total that is exactly the end of a rank counts as the full rank " +
            "(remainder equal to the requirement of that rank), not as the start of the next one. A total past the end of the chart throws.",
            "Totals at, one below and one above the end of ranks {0,1,2,5,10,11,20,21,30,40,50,51,60,70,80,90,99,100}, plus -5, 0, 1, the last total + 1 and int.MaxValue.",
            new[] { "totalXP" },
            new[] { "rank", "remainderXP" });

        var totals = new SortedSet<int> { -5, 0, 1, int.MaxValue, Skill.GetTotalRequiredXP(100) + 1 };
        foreach (var level in new[] { 0, 1, 2, 5, 10, 11, 20, 21, 30, 40, 50, 51, 60, 70, 80, 90, 99, 100 })
        {
            var total = Skill.GetTotalRequiredXP(level);
            totals.Add(total - 1);
            totals.Add(total);
            totals.Add(total + 1);
        }

        foreach (var total in totals)
        {
            var captured = total;
            var result = Outcome.Value(() => Skill.GetLevelByTotalXP(captured));
            if (result is ValueTuple<int, int> (var rank, var remainder))
                table.Add(total, rank, remainder);
            else
                table.Add(total, result, result);
        }

        Snapshot.Verify("Skill", "GetLevelByTotalXP", table);
    }

    [Test]
    public void GetDeltaXP()
    {
        var table = new SnapshotTable(
            "Skill.GetDeltaXP(int delta)",
            "The XP an activity grants for its level minus the skill rank. Differences above the table's highest entry count as that entry; " +
            "differences below the lowest entry grant nothing.",
            "Every difference -10..10 plus int.MinValue and int.MaxValue.",
            new[] { "delta" },
            new[] { "xp" });

        foreach (var delta in Enumerable.Range(-10, 21).Prepend(int.MinValue).Append(int.MaxValue))
            table.Add(delta, Skill.GetDeltaXP(delta));

        Snapshot.Verify("Skill", "GetDeltaXP", table);
    }

    [Test]
    public void GetPracticeXP()
    {
        var table = new SnapshotTable(
            "Skill.GetPracticeXP(int activityLevel, int skillRank, int rankLimit)",
            "Practice XP of a tiered activity: nothing once the skill rank reaches the (exclusive) rank limit, otherwise the rank-difference XP with a floor of 150.",
            "activity level {1,10,25,40,50,53} x skill rank {0,1,9,10,24,25,26,39,40,49,50,60} x rank limit {1,10,26,50}.",
            new[] { "activityLevel", "skillRank", "rankLimit" },
            new[] { "xp" });

        foreach (var row in Grid.Cross(
                     new[] { 1, 10, 25, 40, 50, 53 },
                     new[] { 0, 1, 9, 10, 24, 25, 26, 39, 40, 49, 50, 60 },
                     new[] { 1, 10, 26, 50 }))
        {
            table.Add(row[0], row[1], row[2], Skill.GetPracticeXP(row[0], row[1], row[2]));
        }

        Snapshot.Verify("Skill", "GetPracticeXP", table);
    }

    [Test]
    public void EspionageRanks()
    {
        var table = new SnapshotTable(
            "Skill.GetEspionageRequiredRank(PerkType, int tier) / Skill.GetEspionagePracticeRankLimit(PerkType, int tier)",
            "The skill rank that unlocks each tier of an Espionage profession, and the exclusive rank where practising that tier stops " +
            "(the next tier's unlock, or the Espionage cap of 50 for tier 5). Unknown professions and tiers outside 1..5 throw.",
            "professions {Poisoncraft, Trapcraft, Slicing, Invalid} x tiers 0..6.",
            new[] { "profession", "tier" },
            new[] { "requiredRank", "practiceRankLimit" });

        foreach (var profession in new[] { PerkType.Poisoncraft, PerkType.Trapcraft, PerkType.Slicing, PerkType.Invalid })
        {
            for (var tier = 0; tier <= 6; tier++)
            {
                var capturedProfession = profession;
                var capturedTier = tier;
                table.Add(
                    profession,
                    tier,
                    Outcome.Value(() => Skill.GetEspionageRequiredRank(capturedProfession, capturedTier)),
                    Outcome.Value(() => Skill.GetEspionagePracticeRankLimit(capturedProfession, capturedTier)));
            }
        }

        Snapshot.Verify("Skill", "EspionageRanks", table);
    }

    [Test]
    public void CalculateEspionageXP()
    {
        var table = new SnapshotTable(
            "Skill.CalculateEspionageXP(PerkType profession, int tier, int skillRank)",
            "Practice XP for an Espionage tier: nothing below the unlock rank of the tier or at and above the unlock rank of the next tier, otherwise at least 150.",
            "professions {Poisoncraft, Trapcraft, Slicing} x tiers 1..5 x skill ranks {0,10,15,18,22,28,30,40,42,45,48,49,50,60}.",
            new[] { "profession", "tier", "skillRank" },
            new[] { "xp" });

        var ranks = new[] { 0, 10, 15, 18, 22, 28, 30, 40, 42, 45, 48, 49, 50, 60 };
        foreach (var profession in new[] { PerkType.Poisoncraft, PerkType.Trapcraft, PerkType.Slicing })
        {
            for (var tier = 1; tier <= 5; tier++)
            {
                foreach (var rank in ranks)
                    table.Add(profession, tier, rank, Skill.CalculateEspionageXP(profession, tier, rank));
            }
        }

        Snapshot.Verify("Skill", "CalculateEspionageXP", table);
    }
}
