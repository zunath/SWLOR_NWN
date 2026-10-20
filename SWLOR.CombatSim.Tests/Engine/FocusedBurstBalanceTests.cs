using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Engine;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.CombatSim.Tests.Engine;

[TestFixture]
public class FocusedBurstBalanceTests
{
    [Test]
    public void ProductionChannel_ReportsOpportunityCostAndInterruptedDamageAcrossRanks()
    {
        const int samples = 64;
        const int firstSeed = 20261018;
        var current = AbilityCatalog.Load();
        var immediate = AbilityCatalog.Load();
        foreach (var feat in new[] { FeatType.FocusedBurst1, FeatType.FocusedBurst2, FeatType.FocusedBurst3 })
            immediate.Add(ImmediateCounterfactual(immediate.Get(feat)));

        var rows = new List<object>();
        foreach (var rank in new[] { 10, 30, 50 })
        foreach (var rhythm in new[] { false, true })
        foreach (var variant in new[] { "opening", "sustain", "haste", "slow", "high defense", "high evasion", "mitigation", "single weapon", "double weapon" })
        foreach (var mode in new[] { "channel", "old immediate", "without Focused Burst", "one wave", "two waves" })
        {
            var catalog = mode == "old immediate" ? immediate : current;
            var seconds = variant == "opening" ? 12 : 120;
            var scenario = TwinPistolScenarios.Create(false, rhythm, seconds, rank,
                haste: variant == "haste" ? 50 : variant == "slow" ? -50 : 0, catalog: catalog);
            var spec = scenario.Teams[0].Members[0];
            var burst = spec.Abilities.SingleOrDefault(use => use.Feat.StartsWith("FocusedBurst"));
            burst.Should().NotBeNull("all sampled ranks have Focused Burst unlocked");
            if (mode == "without Focused Burst") spec.Abilities.Remove(burst);
            if (mode is "one wave" or "two waves") burst.ChannelInterruptAfterSeconds = mode == "one wave" ? .3f : .9f;
            if (variant == "single weapon")
            {
                spec.WeaponLoadout.OffhandDamage = 0;
                spec.WeaponLoadout.OffhandDelay = 0;
            }
            if (variant == "double weapon") spec.WeaponLoadout.OffhandSharesMainItem = true;
            var target = scenario.Teams[1].Members[0];
            if (variant == "high defense") target.Defense = 250;
            if (variant == "high evasion") target.Evasion = 250;
            if (variant == "mitigation") target.DamageReductionPercent = 50;
            var actors = Enumerable.Range(firstSeed, samples)
                .Select(seed => new Simulation(scenario, RuleSets.Current(), catalog, seed).Run().Combatants[0]).ToArray();
            var damage = actors.Select(actor => actor.Stats.DamageDealt).ToArray();
            var mean = damage.Average();
            var deviation = Math.Sqrt(damage.Sum(value => Math.Pow(value - mean, 2)) / (samples - 1));
            foreach (var actor in actors)
            {
                actor.Stm.Should().BeInRange(0, spec.MaxStm);
                actor.Stats.StmSpent.Should().BeGreaterThanOrEqualTo(actor.Stats.StmRefunded);
                actor.Stats.AbilityImpacts.Should().BeGreaterThanOrEqualTo(actor.Stats.AbilityHits);
            }
            rows.Add(new
            {
                rank, handling = rhythm ? "Matched Rhythm" : "Paired Handling", variant, mode, seconds, samples,
                firstSeed, lastSeed = firstSeed + samples - 1, skillPoints = catalog.Perks.GetPurchaseCost(spec.Perks),
                damageMean = mean, dps = mean / seconds, damageMean95HalfWidth = 1.96 * deviation / Math.Sqrt(samples),
                ordinaryAttempts = actors.Average(actor => actor.Stats.OrdinaryAttempts),
                ordinaryDamage = actors.Average(actor => actor.Stats.OrdinaryDamage),
                abilityDamage = actors.Average(actor => actor.Stats.AbilityDamage),
                abilityImpacts = actors.Average(actor => actor.Stats.AbilityImpacts),
                staminaSpent = actors.Average(actor => actor.Stats.StmSpent),
                staminaRefunded = actors.Average(actor => actor.Stats.StmRefunded),
                staminaEnd = actors.Average(actor => actor.Stm),
                interrupts = actors.Average(actor => actor.Stats.InterruptedCasts),
                abilityUses = actors.SelectMany(actor => actor.Stats.AbilityUses).GroupBy(entry => entry.Key)
                    .ToDictionary(group => group.Key, group => group.Sum(entry => entry.Value) / (double)samples)
            });
        }
        var directory = Environment.GetEnvironmentVariable("TWIN_SIM_OUTPUT") ?? Path.Combine(Path.GetTempPath(), "swlor-focused-burst-simulator");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "focused-burst-opportunity-cost.json");
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            method = "64 fixed seeds per cell. Current production payload and shared post-roll damage partition. " +
                "Old immediate is an explicit counterfactual: same authored cost/payload/cooldown, one instant paired impact. " +
                "Without Focused Burst retains the same purchased perks and skips that button; points are not redistributed. " +
                "Rank scenarios vary skill gates and activated rank damage bonus while holding ATT150/ACC150/DMG22 per hand/Delay250/ordinary weapon rank50/attributes26 fixed; they are not equipment progression builds. " +
                "Finite 61 STM, 20 STM/30s, targets HP30000/DEF94/EVA134/VIT26 unless varied. " +
                "Channels suppress ordinary attacks and all other ability decisions until the 1.5s tail completes; the production partition divides each independently rolled full magnitude across back-loaded waves. " +
                "Explicit cancels at 0.3/0.9s preserve paid cost/recast, discard remaining waves and retain the ordinary attack cooldown that existed before the channel. " +
                "Logical cadence only: no native input, geometry, animation, projectiles, item procs, resistance conversion or live C# execution. " +
                "Single/double weapon controls exercise cross-skill hand counts, not equal-investment cross-skill builds. " +
                "No equal-SP Pistol comparator: native engine tests cover that family. Results assess revision sensitivity, not global balance proof.",
            partitionOf600Damage = Enumerable.Range(0, 3).Select(index => WeaponImpactDamagePartition.Partition(600, index, 3)).ToArray(),
            rows
        }, new JsonSerializerOptions { WriteIndented = true }));
        TestContext.AddTestAttachment(path);
        TestContext.Progress.WriteLine($"Focused Burst: {rows.Count} cells x {samples} seeds; {path}");
    }

    internal static AbilityInfo ImmediateCounterfactual(AbilityInfo source) => new()
    {
        Feat = source.Feat, Name = source.Name, PerkName = source.PerkName,
        RecastGroup = source.RecastGroup, SkillRequirement = source.SkillRequirement,
        FP = source.FP, STM = source.STM, PaysFpFirst = source.PaysFpFirst,
        RecastSeconds = source.RecastSeconds, CastSeconds = 0, IsChanneled = false,
        ImpactDelaySeconds = source.ImpactDelaySeconds, IsQueuedWeapon = source.IsQueuedWeapon,
        UnreadableTiming = source.UnreadableTiming, SkillType = source.SkillType,
        IsHostile = source.IsHostile, IsAimed = source.IsAimed, IsHealing = source.IsHealing,
        ForceAffinity = source.ForceAffinity,
        TwinPistol = source.TwinPistol with { VolleyCount = 1, VolleyIntervalSeconds = 0, ChannelSeconds = 0 }
    };
}
