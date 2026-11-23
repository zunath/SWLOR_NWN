using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;
using static SWLOR.Game.Server.Tests.Support.CombatTranscriptBench;
using Player = SWLOR.Game.Server.Entity.Player;
using SkillType = SWLOR.Game.Server.Domain.Foundation.SkillType;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>
/// The ordered call-and-draw transcript of <c>GiveSkillXP</c>, taken on the code as it stands: the refusals, the bonuses and the
/// henchman penalty, XP debt repaid in part and in full, the rank ceiling and the once-per-login limit warning, the combined XP
/// message, rank-ups with their skill and ability points, and a rank gained at the skill cap with the random choice of the skill that
/// decays (every <c>Random</c> draw, the refresh notice, the gain and decay events, the cap warnings). Each step writes the engine calls,
/// the database reads and writes, the notices and the draws in the order they happen, then what the award returned and what changed on
/// the record. Regenerate with <c>SWLOR_UPDATE_SNAPSHOTS=1</c> only for a deliberate change.
/// </summary>
[TestFixture]
[NonParallelizable]
public class SkillXPTranscriptTests
{
    private const string FixturePath = "SWLOR.Game.Server.Tests/Fixtures/CharacterTranscripts/SkillXPAward.txt";
    private const string PlayerId = "uuid-Attacker";

    private static readonly Type[] Leaves =
    {
        typeof(IStatService), typeof(IGuiRefreshPublisher), typeof(IPlayerFeedbackService), typeof(ICompanionLookup)
    };

    private CombatTranscriptBench _bench = null!;
    private ISkillService _skill = null!;

    [Test]
    public void GiveSkillXPProducesTheCapturedTranscript()
    {
        _bench = new CombatTranscriptBench(Leaves, (services, bench) =>
            services.Replace(ServiceDescriptor.Singleton<IDBService>(new LoggingDB(bench.Db, step => bench.Lines.Add("Db." + step)))));
        using var _ = _bench;
        _skill = _bench.Get<ISkillService>();
        _skill.CacheData();
        _skill.CacheXPChartData();
        _skill.LoadMappings();
        _bench.Events.SetReturnHandler("SignalEvent", args =>
        {
            _bench.Lines.Add($"Events.SignalEvent({args[0]}, {_bench.Name((uint)args[1]!)})");
            return true;
        });
        _bench.Events.SetReturnHandler("PushEventData", args =>
        {
            _bench.Lines.Add($"Events.PushEventData({args[0]}, {args[1]})");
            return null;
        });
        _bench.Leaf<IGuiRefreshPublisher>().Script("PublishRefreshEvent", args =>
        {
            var skills = ((SkillXPRefreshEvent)args[1]!).ModifiedSkills.Select(skill => skill.ToString());
            _bench.Lines.Add("  refreshed skills: " + string.Join(", ", skills));
            return null;
        });

        Refusals();
        Plain();
        Bonuses();
        Messages();
        Debt();
        Ceiling();
        RankUps();
        AtTheCap();

        // Color tokens carry control characters; the text keeps to ASCII.
        var text = string.Concat(_bench.Text.Select(character => character < 128 ? character.ToString() : $"\\u{(int)character:x4}"));
        CombatTranscriptBench.AssertGolden(FixturePath, text);
    }

    private void Refusals()
    {
        Shape(_ => { });
        _bench.Step("refused: the skill is Invalid", () => Award(SkillType.Invalid, 100));
        _bench.Step("refused: nothing to give", () => Award(SkillType.Armor, 0));
        _bench.Step("refused: the target is not a player", () => Award(SkillType.Armor, 100, target: Other));
        _bench.Step("refused: a DM", () =>
        {
            _bench.Engine.SetReturnHandler("GetIsDM", args => (uint)args[0]! == Attacker);
            Award(SkillType.Armor, 100);
            _bench.Engine.SetReturnHandler("GetIsDM", args => false);
        });
        _bench.Step("refused: dead", () =>
        {
            _bench.Engine.SetReturnHandler("GetIsDead", args => (uint)args[0]! == Attacker);
            Award(SkillType.Armor, 100);
            _bench.Engine.SetReturnHandler("GetIsDead", args => false);
        });
        _bench.Step("refused: no hit points", () =>
        {
            _bench.Engine.SetReturnHandler("GetCurrentHitPoints", args => 0);
            Award(SkillType.Armor, 100);
            _bench.Engine.SetReturnHandler("GetCurrentHitPoints", args => 100);
        });
    }

    private void Plain()
    {
        _bench.Step("plain award with the default henchman penalty", () =>
        {
            Shape(_ => { });
            Award(SkillType.Armor, 100);
        });
        _bench.Step("plain award, bonuses ignored and no penalty", () =>
        {
            Shape(_ => { });
            Award(SkillType.Armor, 100, ignoreBonuses: true, penalty: false);
        });
    }

    private void Bonuses()
    {
        _bench.Step("bonuses: social 4, status adjustment 20, DM bonus 10, with the penalty", () =>
        {
            Shape(player => player.DMXPBonus = 10);
            _bench.Engine.SetReturn("GetAbilityScore", 4);
            _bench.SetStat(Attacker, StatType.ExperiencePercentAdjustment, 20);
            Award(SkillType.Armor, 100);
        });
        _bench.Step("bonuses: social 4, status adjustment 20, DM bonus 10, no penalty", () =>
        {
            Shape(player => player.DMXPBonus = 10);
            Award(SkillType.Armor, 100, penalty: false);
        });
        _bench.Step("bonuses: ignored while the stats stay raised", () =>
        {
            Shape(player => player.DMXPBonus = 10);
            Award(SkillType.Armor, 100, ignoreBonuses: true, penalty: false);
            _bench.Engine.SetReturn("GetAbilityScore", 0);
            _bench.SetStat(Attacker, StatType.ExperiencePercentAdjustment, 0);
        });
    }

    private void Messages()
    {
        _bench.Step("message source: two awards from one source are one message", () =>
        {
            Shape(_ => { });
            Award(SkillType.Armor, 100, ignoreBonuses: true, penalty: false, source: Other);
            Award(SkillType.Armor, 50, ignoreBonuses: true, penalty: false, source: Other);
            Award(SkillType.Piloting, 20, ignoreBonuses: true, penalty: false, source: Other);
        });
    }

    private void Debt()
    {
        _bench.Step("debt: 500 owed, 100 repays part of it", () =>
        {
            Shape(player => player.XPDebt = 500);
            Award(SkillType.Armor, 100, ignoreBonuses: true, penalty: false);
        });
        _bench.Step("debt: 40 owed, 100 clears it and 60 is applied", () =>
        {
            Shape(player => player.XPDebt = 40);
            Award(SkillType.Armor, 100, ignoreBonuses: true, penalty: false);
        });
        _bench.Step("debt: exactly what is owed", () =>
        {
            Shape(player => player.XPDebt = 100);
            Award(SkillType.Armor, 100, ignoreBonuses: true, penalty: false);
        });
    }

    private void Ceiling()
    {
        _bench.Step("ceiling: a maxed skill with leftover progress clears it", () =>
        {
            Shape(player =>
            {
                var skill = player.Skills[SkillType.Armor];
                skill.Rank = MaxRank(SkillType.Armor);
                skill.XP = 30;
            });
            Award(SkillType.Armor, 100, ignoreBonuses: true, penalty: false);
        });
        _bench.Step("ceiling: a skill at its limit warns once per login", () =>
        {
            Shape(player =>
            {
                var skill = player.Skills[SkillType.Armor];
                skill.Rank = 10;
                skill.RankLimit = 10;
                skill.XP = 5;
            });
            Award(SkillType.Armor, 100, ignoreBonuses: true, penalty: false);
            Award(SkillType.Armor, 100, ignoreBonuses: true, penalty: false);
            Award(SkillType.Piloting, 100, ignoreBonuses: true, penalty: false);
        });
        _bench.Step("ceiling: a language at its limit does not warn", () =>
        {
            Shape(player =>
            {
                var skill = player.Skills[SkillType.Mirialan];
                skill.Rank = 0;
                skill.RankLimit = 0;
            });
            Award(SkillType.Mirialan, 100, ignoreBonuses: true, penalty: false);
        });
        _bench.Step("ceiling: debt repaid at the ceiling stays repaid", () =>
        {
            Shape(player =>
            {
                player.XPDebt = 30;
                var skill = player.Skills[SkillType.Armor];
                skill.Rank = MaxRank(SkillType.Armor);
            });
            Award(SkillType.Armor, 100, ignoreBonuses: true, penalty: false);
        });
    }

    private void RankUps()
    {
        var required = _skill.GetRequiredXP(0);
        _bench.Step("rank-up: one rank below the cap", () =>
        {
            Shape(_ => { });
            Award(SkillType.Armor, required + 7, ignoreBonuses: true, penalty: false);
        });
        _bench.Step("rank-up: several ranks in one award", () =>
        {
            Shape(_ => { });
            Award(SkillType.Armor, required + _skill.GetRequiredXP(1) + _skill.GetRequiredXP(2) + 3, ignoreBonuses: true, penalty: false);
        });
        _bench.Step("rank-up: the tenth skill point earns an ability point", () =>
        {
            Shape(player => player.TotalSPAcquired = 9);
            Award(SkillType.Armor, required, ignoreBonuses: true, penalty: false);
        });
        _bench.Step("rank-up: the ability point pool is full", () =>
        {
            Shape(player =>
            {
                player.TotalSPAcquired = 9;
                player.TotalAPAcquired = 40;
            });
            Award(SkillType.Armor, required, ignoreBonuses: true, penalty: false);
        });
        _bench.Step("rank-up: a language earns no skill point", () =>
        {
            Shape(player => player.TotalSPAcquired = 9);
            Award(SkillType.Mirialan, required + 1, ignoreBonuses: true, penalty: false);
        });
        _bench.Step("rank-up: leftover progress is discarded at the ceiling", () =>
        {
            Shape(player => player.Skills[SkillType.Armor].Rank = MaxRank(SkillType.Armor) - 1);
            Award(SkillType.Armor, _skill.GetRequiredXP(MaxRank(SkillType.Armor) - 1) * 3, ignoreBonuses: true, penalty: false);
        });
        _bench.Step("rank-up: a limit lower than the max stops the climb", () =>
        {
            Shape(player =>
            {
                player.Skills[SkillType.Armor].Rank = 4;
                player.Skills[SkillType.Armor].RankLimit = 5;
            });
            Award(SkillType.Armor, _skill.GetRequiredXP(4) * 3, ignoreBonuses: true, penalty: false);
        });
    }

    private void AtTheCap()
    {
        var required = _skill.GetRequiredXP(0);
        _bench.Step("cap: no other skill can decay, the award is refused with a warning", () =>
        {
            Shape(player => FillToCap(player, SkillType.Armor, unlocked: Array.Empty<SkillType>()));
            Award(SkillType.Armor, required * 2, ignoreBonuses: true, penalty: false);
        });
        _bench.Step("cap: refused, with debt repaid by the same award", () =>
        {
            Shape(player =>
            {
                player.XPDebt = 25;
                FillToCap(player, SkillType.Armor, unlocked: Array.Empty<SkillType>());
            });
            Award(SkillType.Armor, required * 2, ignoreBonuses: true, penalty: false);
        });
        _bench.Step("cap: a rank gained decays the only unlocked skill", () =>
        {
            Shape(player => FillToCap(player, SkillType.Armor, unlocked: new[] { SkillType.Piloting }));
            Award(SkillType.Armor, required, ignoreBonuses: true, penalty: false);
        });
        _bench.Step("cap: a rank gained draws one of three decay candidates", () =>
        {
            Shape(player => FillToCap(player, SkillType.Armor, unlocked: new[] { SkillType.Piloting, SkillType.FirstAid, SkillType.Smithery }));
            Award(SkillType.Armor, required, ignoreBonuses: true, penalty: false);
        });
        _bench.Step("cap: three ranks gained draw three times", () =>
        {
            Shape(player => FillToCap(player, SkillType.Armor, unlocked: new[] { SkillType.Piloting, SkillType.FirstAid, SkillType.Smithery, SkillType.Fabrication }));
            Award(SkillType.Armor, required + _skill.GetRequiredXP(1) + _skill.GetRequiredXP(2), ignoreBonuses: true, penalty: false);
        });
        _bench.Step("cap: the same skill decays twice", () =>
        {
            Shape(player => FillToCap(player, SkillType.Armor, unlocked: new[] { SkillType.Piloting }, unlockedRank: 5));
            Award(SkillType.Armor, required + _skill.GetRequiredXP(1), ignoreBonuses: true, penalty: false);
        });
        _bench.Step("cap: the last candidate runs out and the climb stops", () =>
        {
            Shape(player => FillToCap(player, SkillType.Armor, unlocked: new[] { SkillType.Piloting }, unlockedRank: 1));
            Award(SkillType.Armor, required * 4, ignoreBonuses: true, penalty: false);
        });
        _bench.Step("cap: skills above their limit decay first", () =>
        {
            Shape(player =>
            {
                FillToCap(player, SkillType.Armor, unlocked: new[] { SkillType.Piloting, SkillType.FirstAid, SkillType.Smithery }, unlockedRank: 6);
                player.Skills[SkillType.FirstAid].RankLimit = 3;
                player.Skills[SkillType.Smithery].RankLimit = 3;
            });
            Award(SkillType.Armor, required + _skill.GetRequiredXP(1), ignoreBonuses: true, penalty: false);
        });
        _bench.Step("cap: a language at the cap gains ranks and nothing decays", () =>
        {
            Shape(player => FillToCap(player, SkillType.Armor, unlocked: new[] { SkillType.Piloting }));
            Award(SkillType.Mirialan, required * 2, ignoreBonuses: true, penalty: false);
        });
    }

    private int MaxRank(SkillType skill) => _skill.GetSkillDetails(skill).MaxRank;

    /// <summary>Resets the attacker's record to no ranks, no XP, no locks, no limits, no debt and no points, then applies the scenario.</summary>
    private void Shape(Action<Player> scenario)
    {
        var player = _bench.Db.Get<Player>(PlayerId);
        foreach (var skill in Enum.GetValues<SkillType>())
        {
            player.Skills[skill] = new PlayerSkill();
        }

        player.XPDebt = 0;
        player.DMXPBonus = 0;
        player.UnallocatedSP = 0;
        player.TotalSPAcquired = 0;
        player.UnallocatedAP = 0;
        player.TotalAPAcquired = 0;
        scenario(player);
        _bench.Db.Set(player);
    }

    /// <summary>
    /// Puts the skills that count toward the skill cap at the cap: the unlocked ones at <paramref name="unlockedRank"/> and the others
    /// locked at their max rank until the total reaches 400. The skill that gains the rank starts at rank 0.
    /// </summary>
    private void FillToCap(Player player, SkillType target, SkillType[] unlocked, int unlockedRank = 10)
    {
        var contributing = _skill.GetAllContributingSkills().Keys.OrderBy(skill => (int)skill).ToArray();
        var remaining = SkillService.SkillCap;
        foreach (var skill in unlocked)
        {
            player.Skills[skill].Rank = unlockedRank;
            remaining -= unlockedRank;
        }

        foreach (var skill in contributing.Where(skill => skill != target && !unlocked.Contains(skill)))
        {
            var rank = Math.Min(MaxRank(skill), remaining);
            if (rank <= 0)
                break;

            player.Skills[skill].Rank = rank;
            player.Skills[skill].IsLocked = true;
            remaining -= rank;
        }

        if (remaining > 0)
            Assert.Fail($"The fixture cannot fill the cap: {remaining} ranks short.");
    }

    private void Award(SkillType skill, int xp, bool ignoreBonuses = false, bool penalty = true, uint source = 0x7F000000, uint target = Attacker)
    {
        var before = Snapshot();
        _bench.Lines.Add($"-> GiveSkillXP({_bench.Name(target)}, {skill}, {xp}, ignoreBonuses: {ignoreBonuses}, henchmanPenalty: {penalty}, source: {(source == 0x7F000000 ? "none" : _bench.Name(source))})");
        var used = _skill.GiveSkillXP(target, skill, xp, ignoreBonuses, penalty, source);
        _bench.Lines.Add($"<- {used}");
        var after = Snapshot();
        foreach (var line in Difference(before, after))
            _bench.Lines.Add("  " + line);
    }

    private string Snapshot()
    {
        var player = _bench.Db.Get<Player>(PlayerId);
        var text = new StringBuilder();
        text.Append($"debt={player.XPDebt};sp={player.UnallocatedSP}/{player.TotalSPAcquired};ap={player.UnallocatedAP}/{player.TotalAPAcquired}");
        foreach (var (skill, state) in player.Skills.OrderBy(pair => (int)pair.Key))
        {
            if (state.Rank != 0 || state.XP != 0 || state.IsLocked || state.RankLimit != null)
                text.Append($"\n{skill}: rank={state.Rank} xp={state.XP} locked={state.IsLocked} limit={state.RankLimit?.ToString() ?? "none"}");
        }

        return text.ToString();
    }

    private static IEnumerable<string> Difference(string before, string after)
    {
        var was = before.Split('\n');
        var now = after.Split('\n');
        yield return "record: " + now[0];
        var previous = was.Skip(1).ToDictionary(line => line[..line.IndexOf(':')], line => line);
        foreach (var line in now.Skip(1))
        {
            var key = line[..line.IndexOf(':')];
            if (!previous.TryGetValue(key, out var old) || old != line)
                yield return "changed " + line;
        }

        var current = now.Skip(1).Select(line => line[..line.IndexOf(':')]).ToHashSet();
        foreach (var key in previous.Keys.Where(key => !current.Contains(key)))
            yield return "cleared " + key;
    }
}
