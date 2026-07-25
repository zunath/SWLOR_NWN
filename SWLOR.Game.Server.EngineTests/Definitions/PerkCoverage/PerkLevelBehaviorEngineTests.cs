using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    /// <summary>
    /// Live behavioral verification of every perk level's passive effects: for each perk,
    /// at each purchasable level, every stat bonus the perk declares must be returned by
    /// Perk.GetStatBonus for an NPC holding that level (perk-wide bonuses plus the current
    /// level's bonuses - levels replace, they do not stack). Expected values are computed
    /// from the built perk data itself, so no hand-authored expectations can drift.
    /// A canary additionally proves Stat.GetStatAdjustment (the gameplay consumption path)
    /// includes the perk contribution.
    /// </summary>
    public static class PerkLevelBehaviorEngineTests
    {
        [EngineTest("Every perk level's stat bonuses apply to an NPC holding that level", Category = "Perk", TimeoutSeconds = 600f)]
        public static async Task AllPerkLevelStatBonusesApply(EngineTestContext ctx)
        {
            var npc = ctx.SpawnCreature("nw_rat001");
            var failures = new List<string>();
            var perks = Perk.GetAllPerks();
            var levelsVerified = 0;
            var bonusesVerified = 0;
            var canaryDone = false;
            var count = 0;

            foreach (var (perkType, detail) in perks)
            {
                var stats = detail.StatBonuses.Select(b => b.Stat)
                    .Concat(detail.PerkLevels.Values.SelectMany(l => l.StatBonuses.Select(b => b.Stat)))
                    .Distinct()
                    .ToList();

                if (stats.Count == 0)
                    continue;

                foreach (var (level, perkLevel) in detail.PerkLevels.OrderBy(l => l.Key))
                {
                    SetLocalInt(npc, $"PERK_LEVEL_{(int)perkType}", level);

                    foreach (var stat in stats)
                    {
                        // Yield periodically so this sweep stays preemptable by the runner's cooperative timeout
                        // and observes cancellation, so a timed-out sweep settles during the grace period.
                        if (++count % 50 == 0)
                        {
                            await ctx.WaitFrameAsync();
                        }

                        // Mirror of Perk.GetStatBonus semantics: perk-wide bonuses always
                        // apply while the level is > 0; per-level bonuses come only from the
                        // CURRENT level's list.
                        var expected =
                            detail.StatBonuses.Where(b => b.Stat == stat).Sum(b => b.Calculate(npc)) +
                            perkLevel.StatBonuses.Where(b => b.Stat == stat).Sum(b => b.Calculate(npc));

                        var actual = Perk.GetStatBonus(npc, stat);
                        if (actual != expected)
                        {
                            failures.Add($"{perkType} L{level} {stat}: expected {expected}, got {actual}");
                        }
                        else
                        {
                            bonusesVerified++;
                        }

                        // Once, with a real non-zero bonus: prove the gameplay consumption
                        // path (Stat.GetStatAdjustment) includes the perk contribution.
                        if (!canaryDone && expected != 0)
                        {
                            var withPerk = Stat.GetStatAdjustment(npc, stat);
                            SetLocalInt(npc, $"PERK_LEVEL_{(int)perkType}", 0);
                            var withoutPerk = Stat.GetStatAdjustment(npc, stat);
                            SetLocalInt(npc, $"PERK_LEVEL_{(int)perkType}", level);

                            if (withPerk - withoutPerk != expected)
                            {
                                failures.Add($"CANARY {perkType} L{level} {stat}: GetStatAdjustment delta {withPerk - withoutPerk} != perk bonus {expected}");
                            }

                            canaryDone = true;
                        }
                    }

                    levelsVerified++;
                }

                SetLocalInt(npc, $"PERK_LEVEL_{(int)perkType}", 0);
            }

            ctx.SetResultDetail($"{levelsVerified} perk level(s) with stat bonuses verified ({bonusesVerified} stat-bonus value(s)); GetStatAdjustment canary {(canaryDone ? "verified" : "NOT EXERCISED")}; {failures.Count} failure(s).");

            if (!canaryDone)
            {
                failures.Add("no non-zero stat bonus was found to exercise the GetStatAdjustment canary");
            }

            if (failures.Count > 0)
            {
                foreach (var failure in failures)
                {
                    ctx.Log($"PERK LEVEL BEHAVIOR FAILURE - {failure}");
                }

                var preview = string.Join(" | ", failures.Take(5));
                var overflow = failures.Count > 5 ? " | ... see the EngineTest log for the full list" : string.Empty;
                ctx.Fail($"{failures.Count} perk level behavior failure(s): {preview}{overflow}");
            }
        }
    }
}
