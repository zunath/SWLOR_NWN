using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Engine;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.CombatSim.Tests.Engine;

[TestFixture]
public class FocusedBurstProgressionTests
{
    [Test]
    public void AuthoredWeaponTiers_ReportCompleteAndPartialChannelOpportunityCost()
    {
        var completionOnly = Environment.GetEnvironmentVariable("TWIN_PROGRESSION_GUARD_ONLY") == "1";
        var samples = completionOnly ? 1024 : 64;
        const int firstSeed = 20261018;
        var current = AbilityCatalog.Load();
        var immediate = AbilityCatalog.Load();
        foreach (var feat in new[] { FeatType.FocusedBurst1, FeatType.FocusedBurst2, FeatType.FocusedBurst3 })
            immediate.Add(FocusedBurstBalanceTests.ImmediateCounterfactual(immediate.Get(feat)));
        var rows = new List<object>();
        var completionTraces = new Dictionary<(bool Rhythm, string Mode), double[]>();
        foreach (var (rank, damage, source) in new[]
                 {
                     (10, 9, "Equipment - Weapons E184/F184/G184/I184/J184; Module/uti/tit_twinpistol.uti.json"),
                     (30, 16, "Equipment - Weapons E195/F195/G195/I195/J195; Module/uti/proto_twinpistol.uti.json"),
                     (50, 23, "Equipment - Weapons E211/F211/G211/I211/J211; Module/uti/chi_twinpistol.uti.json")
                 })
        foreach (var rhythm in new[] { true }) // Barrage's only handling slot is Matched Rhythm since the Cadence rework.
        foreach (var haste in new[] { 0, 50 })
        foreach (var mode in new[] { "channel", "old immediate", "without Focused Burst", "first-wave cancel", "one wave", "two waves" })
        {
            if (completionOnly && (rank != 10 || haste != 50 || mode is "old immediate" or "without Focused Burst")) continue;
            var catalog = mode == "old immediate" ? immediate : current;
            var scenario = TwinPistolScenarios.Create(false, rhythm, 120, rank, haste: haste, catalog: catalog);
            var spec = scenario.Teams[0].Members[0];
            spec.WeaponLoadout.MainDamage = spec.WeaponLoadout.OffhandDamage = damage;
            spec.WeaponLoadout.WeaponRank = rank;
            spec.WeaponLoadout.Attack = Stat.GetAttack(rank, 26, 0);
            spec.WeaponLoadout.Accuracy = Stat.GetAccuracy(rank, 26, 0);
            var burst = spec.Abilities.Single(use => use.Feat.StartsWith("FocusedBurst"));
            if (mode == "without Focused Burst") spec.Abilities.Remove(burst);
            if (mode == "first-wave cancel") burst.ChannelInterruptAfterSeconds = .05f;
            if (mode is "one wave" or "two waves") burst.ChannelInterruptAfterSeconds = mode == "one wave" ? .3f : .9f;
            var target = scenario.Teams[1].Members[0];
            target.Defense = Stat.CalculateDefense(26, rank, 0);
            target.Evasion = Stat.GetEvasion(rank, 26, 0);
            var actors = Enumerable.Range(firstSeed, samples)
                .Select(seed => new Simulation(scenario, RuleSets.Current(), catalog, seed).Run().Combatants[0]).ToArray();
            var damageRolls = actors.Select(actor => actor.Stats.DamageDealt).ToArray();
            var mean = damageRolls.Average();
            var deviation = Math.Sqrt(damageRolls.Sum(value => Math.Pow(value - mean, 2)) / (samples - 1));
            actors.Should().OnlyContain(actor => actor.Stm >= 0 && actor.Stm <= spec.MaxStm);
            if (rank == 10 && haste == 50) completionTraces[(rhythm, mode)] = damageRolls;
            rows.Add(new
            {
                rank, weaponDamage = damage, weaponSource = source, delay = 250,
                attack = spec.WeaponLoadout.Attack, accuracy = spec.WeaponLoadout.Accuracy,
                defense = target.Defense, evasion = target.Evasion,
                handling = rhythm ? "Matched Rhythm" : "Steady Cadence", haste, mode,
                seconds = 120, samples, firstSeed, lastSeed = firstSeed + samples - 1,
                lineSkillPoints = catalog.Perks.GetPurchaseCost(spec.Perks), armorSkillPoints = 9,
                damageMean = mean, dps = mean / 120, damageMean95HalfWidth = 1.96 * deviation / Math.Sqrt(samples),
                ordinaryAttempts = actors.Average(actor => actor.Stats.OrdinaryAttempts),
                ordinaryDamage = actors.Average(actor => actor.Stats.OrdinaryDamage),
                abilityDamage = actors.Average(actor => actor.Stats.AbilityDamage),
                staminaSpent = actors.Average(actor => actor.Stats.StmSpent),
                staminaRefunded = actors.Average(actor => actor.Stats.StmRefunded),
                abilityUses = actors.SelectMany(actor => actor.Stats.AbilityUses).GroupBy(entry => entry.Key)
                    .ToDictionary(group => group.Key, group => group.Sum(entry => entry.Value) / (double)samples)
            });
        }
        var completionComparisons = new List<(bool Rhythm, string Partial, double MeanDpsDifference, double HalfWidth95)>();
        foreach (var rhythm in new[] { true }) // Barrage's only handling slot is Matched Rhythm since the Cadence rework.
        foreach (var partial in new[] { "first-wave cancel", "one wave", "two waves" })
        {
            var differences = completionTraces[(rhythm, "channel")]
                .Zip(completionTraces[(rhythm, partial)], (full, cancelled) => (full - cancelled) / 120).ToArray();
            var mean = differences.Average();
            var deviation = Math.Sqrt(differences.Sum(value => Math.Pow(value - mean, 2)) / (samples - 1));
            completionComparisons.Add((rhythm, partial, mean, 1.96 * deviation / Math.Sqrt(samples)));
        }
        var directory = Environment.GetEnvironmentVariable("TWIN_SIM_OUTPUT") ?? Path.Combine(Path.GetTempPath(), "swlor-focused-burst-simulator");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, completionOnly ? "focused-burst-completion-refinement.json" : "focused-burst-authored-tiers.json");
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            method = $"{samples} seeds per cell. Authored Twin item DMG and Delay match source Pistol rows in Equipment - Weapons; TwinPistolRecipes copies Pistol profiles. " +
                "Skill/ordinary weapon rank 10/30/50, all attributes 26 (Character Stats A1 legal upper-attribute control), no ATT/ACC gear bonus; production Stat formulas derive attacker and same-rank defender ratings. " +
                "The neutral defender is a formula comparison, not an authored NPC encounter. Fixed 61 STM/20 STM regeneration per 30s, no item procs, and extra 9 SP Armor offhand 30% investment at Armor rank 40 (General B10/G10/D10 through B12/G12/D12) as the existing benchmark. " +
                "Line points follow actual rank-gated purchases and stay the same within each mode; omitted Focused Burst points are not redistributed. " +
                "External haste 0/50 is an explicit sensitivity input, not a claim of a standalone low-rank build. No weapon enhancements, outgoing/readiness bonuses, conversion or typed resistance. " +
                "Full channel 1.5s, three back-loaded partitioned paired waves at 0/.6/1.2s; the production helper partitions each independently rolled full magnitude. " +
                "Partial cancels at 0.05/0.3/0.9s retain full paid cost/recast and drop remaining waves; native channel completion/cancellation resumes attacks immediately, preserving the pre-channel ordinary swing cooldown. " +
                "Completion comparisons pair damage differences by seed; the guard flags a statistically clear cancellation advantage when the entire 95% interval is below zero. " +
                "Results validate authored-tier opportunity cost, not live terrain, native input/animation/projectiles, cross-skill procs or global balance.",
            partitionOf600Damage = Enumerable.Range(0, 3).Select(index => WeaponImpactDamagePartition.Partition(600, index, 3)).ToArray(),
            completionComparisons = completionComparisons.Select(result => new
            {
                handling = result.Rhythm ? "Matched Rhythm" : "Steady Cadence", partial = result.Partial,
                meanFullMinusPartialDps = result.MeanDpsDifference,
                paired95HalfWidth = result.HalfWidth95,
                statisticallyClearPartialAdvantage = result.MeanDpsDifference + result.HalfWidth95 < 0
            }),
            rows
        }, new JsonSerializerOptions { WriteIndented = true }));
        TestContext.AddTestAttachment(path);
        TestContext.Progress.WriteLine($"Focused Burst authored tiers: {rows.Count} cells x {samples} seeds; {path}");
        foreach (var result in completionComparisons)
            (result.MeanDpsDifference + result.HalfWidth95).Should().BeGreaterThanOrEqualTo(0,
                $"rank 10 at maximum haste must not show a statistically clear {result.Partial} advantage over completion ({(result.Rhythm ? "Matched Rhythm" : "Steady Cadence")})");
    }
}
