using System.Reflection;
using NUnit.Framework;
using SWLOR.Game.Server.CharacterizationTests.Definitions;
using SWLOR.Game.Server.CharacterizationTests.Support;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using GameRandom = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.CharacterizationTests.Service;

/// <summary>
/// Locks the rank rules of the skill XP award that work on a <see cref="Player"/> record without the engine: skill point
/// totals, rank limits and ceilings, the total-rank skill cap, which skills may decay when the cap is reached, and how
/// much XP a skill can still take. <c>Skill.GiveSkillXP</c> itself needs the engine (Tier B); these are the rules it applies.
/// </summary>
[NonParallelizable]
public class SkillRankRuleCharacterizationTests
{
    private const int DecayPickSeed = 4101;
    private const int DecayPickDraws = 24;

    private static readonly MethodInfo DecayCandidatesMethod =
        typeof(Skill).GetMethod("GetDecayCandidates", BindingFlags.Static | BindingFlags.NonPublic)!;

    private static readonly MethodInfo DecayCapacityMethod =
        typeof(Skill).GetMethod("GetDecayCapacity", BindingFlags.Static | BindingFlags.NonPublic)!;

    private static readonly SkillType[] CappedFillers =
    {
        SkillType.Piloting, SkillType.FirstAid, SkillType.Smithery, SkillType.Fabrication, SkillType.Gathering, SkillType.Leadership,
        SkillType.BeastMastery, SkillType.Force
    };

    /// <summary>One award situation: the skill that would gain XP and the other skills of the player, in record order.</summary>
    private sealed record Scenario(
        string Name,
        SkillType Skill,
        PlayerSkill Target,
        params (SkillType Skill, PlayerSkill State)[] Others);

    private static PlayerSkill State(int rank, int xp = 0, bool locked = false, int? limit = null) =>
        new() { Rank = rank, XP = xp, IsLocked = locked, RankLimit = limit };

    private static Scenario[] Scenarios()
    {
        // Eight filler skills make the total easy to place against the cap of 400.
        (SkillType, PlayerSkill)[] Fillers(int rank, bool locked = false, int? limit = null, int count = 8) =>
            CappedFillers.Take(count).Select(skill => (skill, State(rank, 0, locked, limit))).ToArray();

        return new[]
        {
            new Scenario("fresh character", SkillType.Armor, State(0)),
            new Scenario("mid rank with progress", SkillType.Armor, State(10, 100), (SkillType.Piloting, State(20))),
            new Scenario("one rank below the maximum", SkillType.Armor, State(49, 300)),
            new Scenario("at the maximum rank", SkillType.Armor, State(50)),
            new Scenario("limit above rank", SkillType.Armor, State(10, 5, limit: 20)),
            new Scenario("at its limit", SkillType.Armor, State(20, 0, limit: 20)),
            new Scenario("above its limit", SkillType.Armor, State(25, 0, limit: 20)),
            new Scenario("limit of zero", SkillType.Armor, State(0, 0, limit: 0)),
            new Scenario("language below its maximum", SkillType.Mirialan, State(5, 10)),
            new Scenario("language at its maximum", SkillType.Mirialan, State(20)),
            // Eight fillers at rank 49 plus the target at rank 8 reach the cap of 400 exactly.
            new Scenario("room under the cap (total 396)", SkillType.Armor, State(5), Fillers(49, count: 7).Append((SkillType.Force, State(48))).ToArray()),
            new Scenario("at the cap, others locked", SkillType.Armor, State(8), Fillers(49, locked: true)),
            new Scenario("at the cap, others at their limit", SkillType.Armor, State(8), Fillers(49, limit: 49)),
            new Scenario("at the cap, others decayable", SkillType.Armor, State(8), Fillers(49)),
            new Scenario("at the cap, one decayable skill", SkillType.Armor, State(8), Fillers(49, locked: true, count: 7).Append((SkillType.Force, State(49))).ToArray()),
            new Scenario("at the cap, one skill above its limit", SkillType.Armor, State(8),
                Fillers(49, count: 7).Append((SkillType.Force, State(49, limit: 40))).ToArray()),
            new Scenario("at the cap, three decayable skills", SkillType.Armor, State(8),
                CappedFillers.Select((skill, index) => (skill, State(49, locked: index < 5))).ToArray()),
            new Scenario("at the cap, two skills above their limit", SkillType.Armor, State(8),
                CappedFillers.Select((skill, index) => (skill, State(49, limit: index < 2 ? 40 : null))).ToArray()),
            new Scenario("at the cap, a skill at rank zero is not a candidate", SkillType.Armor, State(8),
                Fillers(49, locked: true, count: 7).Append((SkillType.Force, State(49, locked: true))).Append((SkillType.Unarmed, State(0))).ToArray()),
            new Scenario("one free rank, stored XP already pays for it", SkillType.Armor, State(8, Skill.GetRequiredXP(8) + 10),
                Fillers(49, locked: true, count: 7).Append((SkillType.Force, State(48, locked: true))).ToArray()),
            new Scenario("language at full total", SkillType.Mirialan, State(5), Fillers(50)),
            new Scenario("over the cap", SkillType.Armor, State(8), Fillers(50, locked: true)),
        };
    }

    private static Player Build(Scenario scenario)
    {
        var player = new Player();
        foreach (var (skill, state) in scenario.Others)
            player.Skills[skill] = state;

        player.Skills[scenario.Skill] = scenario.Target;
        return player;
    }

    [Test]
    public void GetTotalSkillPoints()
    {
        var table = new SnapshotTable(
            "Skill.GetTotalSkillPoints(Player)",
            "The skill points a player can spend: the starting points plus the earned points, with the earned points limited to 0..400.",
            "TotalSPAcquired {-1000,-1,0,1,9,10,11,100,399,400,401,1000,int.MaxValue}.",
            new[] { "totalSPAcquired" },
            new[] { "totalSkillPoints" });

        foreach (var earned in new[] { -1000, -1, 0, 1, 9, 10, 11, 100, 399, 400, 401, 1000, int.MaxValue })
            table.Add(earned, Skill.GetTotalSkillPoints(new Player { TotalSPAcquired = earned }));

        Snapshot.Verify("Skill", "GetTotalSkillPoints", table);
    }

    [Test]
    public void GetRankLimit_AndGetRankCeiling()
    {
        var table = new SnapshotTable(
            "Skill.GetRankLimit(PlayerSkill, SkillAttribute) / Skill.GetRankCeiling(PlayerSkill, SkillAttribute)",
            "The rank limit a player set on a skill, clamped to 0..MaxRank (null when none is set), and the rank where the skill stops gaining XP " +
            "(the limit, or the maximum rank when there is no limit).",
            "rank limit {null,-10,-1,0,1,19,20,21,50,100,500} x skill maximum rank {1,20,50,100}. A null limit means no limit.",
            new[] { "rankLimit", "maxRank" },
            new[] { "rankLimitResult", "rankCeiling" });

        foreach (var limit in new int?[] { null, -10, -1, 0, 1, 19, 20, 21, 50, 100, 500 })
        {
            foreach (var maxRank in new[] { 1, 20, 50, 100 })
            {
                var state = State(0, 0, false, limit);
                var details = new SkillAttribute(SkillCategoryType.Combat, "Test", maxRank, true, "Test.", true, false, false);
                table.Add(limit, maxRank, Skill.GetRankLimit(state, details), Skill.GetRankCeiling(state, details));
            }
        }

        Snapshot.Verify("Skill", "GetRankLimit", table);
    }

    [Test]
    public void GetTotalContributingSkillRanks()
    {
        var table = new SnapshotTable(
            "Skill.GetTotalContributingSkillRanks(Player)",
            "The sum of the ranks of the skills that count toward the skill cap; languages do not count and a negative rank counts as zero.",
            "Hand-built player records.",
            new[] { "case" },
            new[] { "totalRanks" });

        var cases = new (string Name, Dictionary<SkillType, PlayerSkill> Skills)[]
        {
            ("no skills", new()),
            ("one contributing skill", new() { [SkillType.Armor] = State(12) }),
            ("two contributing skills", new() { [SkillType.Armor] = State(12), [SkillType.Piloting] = State(30) }),
            ("languages are ignored", new() { [SkillType.Armor] = State(12), [SkillType.Mirialan] = State(15) }),
            ("negative rank counts as zero", new() { [SkillType.Armor] = State(12), [SkillType.Piloting] = State(-5) }),
            ("all contributing skills at 50", Enum.GetValues<SkillType>()
                .Where(skill => skill != SkillType.Invalid && skill.GetAttribute<SkillType, SkillAttribute>().ContributesToSkillCap)
                .ToDictionary(skill => skill, _ => State(50))),
        };

        foreach (var (name, skills) in cases)
        {
            var player = new Player();
            foreach (var (skill, state) in skills)
                player.Skills[skill] = state;

            table.Add(name, Skill.GetTotalContributingSkillRanks(player));
        }

        Snapshot.Verify("Skill", "GetTotalContributingSkillRanks", table);
    }

    [Test]
    public void SkillCapRules()
    {
        using var cache = new SkillCacheScope();
        var scenarios = Scenarios();

        var table = new SnapshotTable(
            "Skill.GetTotalContributingSkillRanks / Skill.IsBlockedBySkillCap / Skill.GetMaxDistributableXP(Player, SkillType) and the private GetDecayCandidates / GetDecayCapacity",
            "For a skill about to gain XP: the total ranks counted against the cap of 400, whether the cap blocks the award (at the cap with no skill left to decay), " +
            "the skills that decay first (unlocked skills above their limit, otherwise unlocked skills without a limit), the ranks decay can take, and the most XP " +
            "the skill can take before any would be lost. The two private methods are read by reflection.",
            "Hand-built player records, one per row; see the case column. The target is Armor unless the case says language (Mirialan). Fillers are Piloting, First Aid, Smithery, Fabrication, " +
            "Gathering, Leadership, Beast Mastery and Force, in that record order. A candidate list is in record order.",
            new[] { "case", "skill", "rank", "xp", "limit" },
            new[] { "totalRanks", "isBlocked", "decayCandidates", "decayCapacity", "maxDistributableXP" })
        {
            AllowConstantOutputs = true
        };

        foreach (var scenario in scenarios)
        {
            var player = Build(scenario);
            var candidates = ((List<SkillType>)DecayCandidatesMethod.Invoke(null, new object[] { player, scenario.Skill })!)
                .Select(skill => DefinitionCell.Text(skill))
                .ToArray();
            table.Add(
                scenario.Name,
                scenario.Skill,
                scenario.Target.Rank,
                scenario.Target.XP,
                scenario.Target.RankLimit,
                Skill.GetTotalContributingSkillRanks(player),
                Skill.IsBlockedBySkillCap(player, scenario.Skill),
                candidates,
                (int)DecayCapacityMethod.Invoke(null, new object[] { player, scenario.Skill })!,
                Skill.GetMaxDistributableXP(player, scenario.Skill));
        }

        Snapshot.Verify("Skill", "SkillCapRules", table);
    }

    [Test]
    public void GetMaxDistributableXP_ForInvalidSkill()
    {
        using var cache = new SkillCacheScope();
        Assert.That(Skill.GetMaxDistributableXP(new Player(), SkillType.Invalid), Is.EqualTo(0));
    }

    [Test]
    public void SkillDecayPick()
    {
        using var cache = new SkillCacheScope();
        using var random = new SeededRandom(DecayPickSeed);

        var table = new SnapshotTable(
            "Random.Next(count) over Skill.GetDecayCandidates (the decay pick in Skill.GiveSkillXP)",
            "When a rank-up pushes the player over the skill cap, GiveSkillXP removes a rank from one decay candidate chosen with Random.Next(candidates.Count). " +
            "GiveSkillXP needs the engine (Tier B), so this records the candidate lists (read from the private GetDecayCandidates) and the same pick expression " +
            "candidates[Random.Next(candidates.Count)] drawn from a seeded generator.",
            $"The cases of SkillCapRules that have at least two decay candidates and a skill that counts toward the cap; {DecayPickDraws} consecutive picks per case from a generator reseeded with {DecayPickSeed} for each case.",
            new[] { "case" },
            new[] { "candidateCount", "picks" },
            DecayPickSeed)
        {
            AllowConstantOutputs = true
        };

        foreach (var scenario in Scenarios())
        {
            var player = Build(scenario);
            var candidates = (List<SkillType>)DecayCandidatesMethod.Invoke(null, new object[] { player, scenario.Skill })!;
            if (candidates.Count < 2 || scenario.Skill == SkillType.Mirialan)
                continue;

            random.Reseed(DecayPickSeed);
            var picks = new List<string>();
            for (var draw = 0; draw < DecayPickDraws; draw++)
                picks.Add(DefinitionCell.Text(candidates[GameRandom.Next(candidates.Count)]));

            table.Add(scenario.Name, candidates.Count, picks);
        }

        Snapshot.Verify("Skill", "SkillDecayPick", table);
    }

    [Test]
    public void SkillXPMessageQueue_CombinesFeedback()
    {
        var table = new SnapshotTable(
            "SkillXPMessageQueue.Add(player, source, skill, xp)",
            "Combines the XP feedback for the same player, source and skill into one message sent when the scheduled callback runs; amounts of zero or less are ignored. " +
            "Each combination schedules one callback, and an add after the callback has run starts a new message.",
            "Scripted add calls (player/source/skill/xp), each script followed by running the scheduled callbacks in order. A | in the script runs the callbacks scheduled so far before the next add.",
            new[] { "case", "script" },
            new[] { "scheduledCallbacks", "messagesSent" });

        var cases = new (string Name, string Script)[]
        {
            ("single add", "1/0/Armor/5"),
            ("zero ignored", "1/0/Armor/0"),
            ("negative ignored", "1/0/Armor/-3"),
            ("same key combines", "1/0/Armor/5 1/0/Armor/7 1/0/Armor/1"),
            ("different skill", "1/0/Armor/5 1/0/Piloting/7"),
            ("different source", "1/0/Armor/5 1/9/Armor/7"),
            ("different player", "1/0/Armor/5 2/0/Armor/7"),
            ("add after the callback ran", "1/0/Armor/5 | 1/0/Armor/7"),
            ("ignored add between combined adds", "1/0/Armor/5 1/0/Armor/0 1/0/Armor/2"),
            ("interleaved keys keep scheduling order", "1/0/Armor/1 2/0/Armor/2 1/0/Armor/3 2/0/Armor/4"),
        };

        foreach (var (name, script) in cases)
        {
            var scheduled = new List<Action>();
            var sent = new List<string>();
            var queue = new SkillXPMessageQueue(scheduled.Add, (player, skill, xp) => sent.Add($"{player}/{skill}/{xp}"));
            var scheduleCalls = 0;
            var run = 0;

            void RunScheduled()
            {
                while (run < scheduled.Count)
                    scheduled[run++]();
            }

            foreach (var step in script.Split(' '))
            {
                if (step == "|")
                {
                    RunScheduled();
                    continue;
                }

                var parts = step.Split('/');
                queue.Add(uint.Parse(parts[0]), uint.Parse(parts[1]), Enum.Parse<SkillType>(parts[2]), int.Parse(parts[3]));
                scheduleCalls = scheduled.Count;
            }

            RunScheduled();
            table.Add(name, script, scheduleCalls, sent);
        }

        Snapshot.Verify("Skill", "SkillXPMessageQueue", table);
    }
}
