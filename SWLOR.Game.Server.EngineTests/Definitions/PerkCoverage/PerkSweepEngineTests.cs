using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    /// <summary>
    /// Live sweep over every registered perk and every ability->perk reference: NPC
    /// perk-level resolution must never throw (the HackingBlade class of bug: an ability
    /// referencing a perk with no definition crashed Perk.GetPerkLevel for NPC activators),
    /// and the PERK_LEVEL local cap must round-trip for every perk.
    /// </summary>
    public static class PerkSweepEngineTests
    {
        [EngineTest("Every registered perk and ability perk reference resolves for NPC activators", Category = "Perk", TimeoutSeconds = 300f)]
        public static async Task AllPerksResolveForNPCActivators(EngineTestContext ctx)
        {
            var npc = ctx.SpawnCreature("nw_rat001");
            var failures = new List<string>();
            var perks = Perk.GetAllPerks();
            var count = 0;

            foreach (var (perkType, _) in perks)
            {
                // Yield periodically so this sweep stays preemptable by the runner's cooperative timeout
                // and observes cancellation, so a timed-out sweep settles during the grace period.
                if (++count % 50 == 0)
                {
                    await ctx.WaitFrameAsync();
                }

                try
                {
                    Perk.GetPerkLevel(npc, perkType);

                    ctx.SetNPCPerkLevel(npc, perkType, 1);
                    var capped = Perk.GetPerkLevel(npc, perkType);
                    if (capped != 1)
                    {
                        failures.Add($"{perkType}: PERK_LEVEL local cap round-trip returned {capped}");
                    }

                    SetLocalInt(npc, $"PERK_LEVEL_{(int)perkType}", 0);
                }
                catch (Exception ex)
                {
                    failures.Add($"{perkType}: {ex.GetType().Name}: {ex.Message}");
                }
            }

            // Structure comparison against the BUILT perk data (PerkBuilder.Build() needs
            // engine 2DA access, so this cannot run in plain NUnit): every coverage case's
            // declared level count, prices, and granted feats must match reality, and every
            // registered perk must have a case.
            var cases = LoadAllCoverageCases();
            var casesByPerk = cases.ToDictionary(c => c.Perk);

            foreach (var (perkType, detail) in perks)
            {
                // Yield periodically so this sweep stays preemptable by the runner's cooperative timeout
                // and observes cancellation, so a timed-out sweep settles during the grace period.
                if (++count % 50 == 0)
                {
                    await ctx.WaitFrameAsync();
                }

                if (!casesByPerk.TryGetValue(perkType, out var coverageCase))
                {
                    failures.Add($"{perkType}: no PerkCoverageCase declared");
                    continue;
                }

                var levels = detail.PerkLevels.OrderBy(l => l.Key).Select(l => l.Value).ToList();

                if (levels.Count != coverageCase.MaxLevel)
                {
                    failures.Add($"{perkType}: case declares {coverageCase.MaxLevel} level(s), built perk has {levels.Count}");
                }

                var actualPrices = levels.Select(l => l.Price).ToArray();
                if (!actualPrices.SequenceEqual(coverageCase.Prices))
                {
                    failures.Add($"{perkType}: case prices [{string.Join(",", coverageCase.Prices)}] != built [{string.Join(",", actualPrices)}]");
                }

                var actualFeats = levels.SelectMany(l => l.GrantedFeats).ToArray();
                if (!actualFeats.SequenceEqual(coverageCase.GrantedFeats))
                {
                    failures.Add($"{perkType}: case feats [{string.Join(",", coverageCase.GrantedFeats)}] != built [{string.Join(",", actualFeats)}]");
                }

                foreach (var level in levels)
                {
                    foreach (var bonus in level.StatBonuses)
                    {
                        if (!Enum.IsDefined(typeof(SWLOR.Game.Server.Service.StatService.StatType), bonus.Stat))
                        {
                            failures.Add($"{perkType}: undefined StatType {bonus.Stat} in a stat bonus");
                        }
                    }
                }
            }

            var referencedPerks = Ability.GetAllAbilityDetails()
                .Select(pair => (Feat: pair.Key, Perk: pair.Value.EffectiveLevelPerkType))
                .Where(x => x.Perk != PerkType.Invalid)
                .ToList();

            foreach (var (feat, perkType) in referencedPerks)
            {
                // Yield periodically so this sweep stays preemptable by the runner's cooperative timeout
                // and observes cancellation, so a timed-out sweep settles during the grace period.
                if (++count % 50 == 0)
                {
                    await ctx.WaitFrameAsync();
                }

                if (!perks.ContainsKey(perkType))
                {
                    failures.Add($"ability {feat} references unregistered perk {perkType}");
                    continue;
                }

                try
                {
                    Perk.GetPerkLevel(npc, perkType);
                }
                catch (Exception ex)
                {
                    failures.Add($"ability {feat} -> {perkType}: {ex.GetType().Name}: {ex.Message}");
                }
            }

            ctx.SetResultDetail($"{perks.Count} perk(s) structurally verified against {cases.Count} coverage case(s); {referencedPerks.Count} ability perk reference(s) swept; {failures.Count} failure(s).");

            if (failures.Count > 0)
            {
                var preview = string.Join(" | ", failures.Take(5));
                var overflow = failures.Count > 5 ? " | ... see the EngineTest log for the full list" : string.Empty;
                foreach (var failure in failures)
                {
                    ctx.Log($"PERK SWEEP FAILURE - {failure}");
                }

                ctx.Fail($"{failures.Count} perk sweep failure(s): {preview}{overflow}");
            }
        }

        private static List<PerkCoverageCase> LoadAllCoverageCases()
        {
            return typeof(IPerkCoverageSource).Assembly
                .GetTypes()
                .Where(t => typeof(IPerkCoverageSource).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .Select(t => (IPerkCoverageSource)Activator.CreateInstance(t))
                .SelectMany(s => s.BuildCases())
                .ToList();
        }
    }
}
