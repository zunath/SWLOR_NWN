using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Engine;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;
using SWLOR.CombatSim.Tests.Support;

namespace SWLOR.CombatSim.Tests.Engine;

[TestFixture]
public class CrossSkillWeaponBalanceTests
{
    [Test]
    public void AuthoredChiroLoadouts_ReportNativeFlowBudgetAndAccuracySensitivity()
    {
        const int samples = 64;
        var catalog = AbilityCatalog.Load();
        var rows = new List<object>();
        foreach (var loadout in Loadouts())
        foreach (var ability in new[] { ("PairedVolley:4", "PairedVolley4"), ("FocusedBurst:3", "FocusedBurst3") })
        foreach (var flow in new[] { 0, 3 })
        {
            var damage = new List<double>();
            var budget = new List<int>();
            var paid = new List<double>();
            var refunded = new List<double>();
            foreach (var seed in Enumerable.Range(20261018, samples))
            {
                var scenario = TwinPistolScenarios.Create(false, false, duration: 5);
                var spec = scenario.Teams[0].Members[0];
                spec.Perks = new() { ability.Item1, "TwinPistolMomentum", "TwinPistolFollowThrough", "BalancedGrip" };
                spec.Abilities = new() { new() { Feat = ability.Item2, MinIntervalSeconds = 600 } };
                spec.StmRegenPer30Seconds = 0;
                spec.WeaponLoadout = loadout.Create();
                var sim = TestScenarios.Spawn(scenario, RuleSets.Current(), catalog, seed);
                var actor = sim.Combatants[0];
                actor.NextActionAt = 0; actor.NextAttackAt = double.PositiveInfinity;
                for (var i = 0; i < flow; i++) { sim.RecordWeaponMovement(actor, 2); sim.RecordLandedWeaponHit(actor); }
                SimulationClock.RunUntil(sim, () => actor.Stats.AbilitiesUsed == 1 && actor.Casting == null, 5, "one complete activation");
                // Twin Pistol builders keep Flow for their finishers, so only a Flow-spending ability applies it.
                var spendsFlow = catalog.Get(Enum.Parse<SWLOR.NWN.API.NWScript.Enum.FeatType>(ability.Item2)).TwinPistol?.SpendsMomentum != false;
                actor.Stats.FlowDamagePowerApplied.Should().Be(spendsFlow ? flow * 2 * loadout.Create().DistinctAttackItemCount : 0);
                actor.Stats.StmRefunded.Should().BeLessThan(actor.Stats.StmSpent);
                damage.Add(actor.Stats.AbilityDamage); budget.Add(actor.Stats.FlowDamagePowerApplied);
                paid.Add(actor.Stats.StmSpent); refunded.Add(actor.Stats.StmRefunded);
            }
            var mean = damage.Average();
            rows.Add(new { loadout = loadout.Name, ability = ability.Item2, flow,
                source = loadout.Source, weapon = loadout.Create(), samples,
                damageMean = mean, damageMean95HalfWidth = 1.96 * Math.Sqrt(damage.Sum(value => Math.Pow(value - mean, 2)) / (samples - 1)) / Math.Sqrt(samples),
                flowRawDamageBudget = budget.Average(), staminaPaid = paid.Average(), staminaRefunded = refunded.Average() });
        }
        var directory = Environment.GetEnvironmentVariable("TWIN_SIM_OUTPUT") ?? Path.Combine(Path.GetTempPath(), "swlor-twin-cross-skill-sim-20261018");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "cross-skill-chiro-loadouts.json");
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            method = "64 fixed paired seeds per cell, native Twin payloads and shared combat math, one complete paid activation with ordinary attacks disabled. " +
                "All compared profiles purchase the same active/Flow/Follow Through/Balanced Grip perks, and vary only pre-existing 0 or 3 Flow. " +
                "Flow is generated through three explicit completed 2m movement+landed-weapon-hit events immediately before activation; this is availability sensitivity, not ambient movement or sustain DPS. " +
                "Real Bible Equipment - Weapons Chiro DMG/delay, rank50 and attributes26; neutral ATT134/ACC134, target DEF94/EVA134/VIT26. " +
                "Balanced Grip adds5 ACC to every actual attack item; natural Single Weapon20% and optional max Doublehand60% total modify main DMG rating before rolls. " +
                "Doublehand III costs9 Armor SP; paired/double profiles explicitly assume the separate9 Armor SP offhand-delay reduction. " +
                "These are legal loadout sensitivities, not equal-SP or complete equipment builds; purchased attributes26 is an upper-attribute control. " +
                "Two-handed/double weapons are one real item, paired weapons two. Flow contributes one12-DMG budget per distinct item, partitioned across Focused Burst's three waves, with one capped Follow Through refund. " +
                "No geometry, client animation, resistance conversion, item procs, foreign passive families or global cross-skill balance claim.",
            rows
        }, new JsonSerializerOptions { WriteIndented = true }));
        TestContext.AddTestAttachment(path);
        TestContext.Progress.WriteLine($"Cross-skill Chiro: {rows.Count} cells x {samples} seeds; {path}");
    }

    private sealed record Loadout(string Name, int Damage, int Delay, string Source, int SingleWeapon = 0, bool Paired = false, bool Double = false)
    {
        public WeaponLoadoutSpec Create() => new()
        {
            MainDamage = Damage, MainDelay = Delay, OffhandDamage = Paired || Double ? Damage : 0,
            OffhandDelay = Paired || Double ? Delay : 0, OffhandSharesMainItem = Double,
            OffhandDelayReductionPercent = Paired || Double ? 30 : 0,
            SingleWeaponDamagePercent = SingleWeapon, Attack = 134, Accuracy = 134,
            SkillRank = 50, WeaponRank = 50, DamageAbility = 26, Perception = 26
        };
    }

    private static IEnumerable<Loadout> Loadouts()
    {
        yield return new("Pistol / natural Single Weapon", 23, 250, "Equipment - Weapons F211/G211, E211=50; native Single Weapon20%", 20);
        yield return new("Pistol / Doublehand III", 23, 250, "Equipment - Weapons F211/G211; native Single Weapon20%+Doublehand40%", 60);
        yield return new("Longsword / natural Single Weapon", 24, 230, "Equipment - Weapons F471/G471, E471=50; native Single Weapon20%", 20);
        yield return new("Longsword / Doublehand III", 24, 230, "Equipment - Weapons F471/G471; native Single Weapon20%+Doublehand40%", 60);
        yield return new("Great Sword", 44, 300, "Equipment - Weapons F38/G38, E38=50");
        yield return new("Rifle", 40, 300, "Equipment - Weapons F250/G250, E250=50");
        yield return new("Paired Pistols", 23, 250, "Equipment - Weapons F211/G211 x2", Paired: true);
        yield return new("Paired Longswords", 24, 230, "Equipment - Weapons F471/G471 x2", Paired: true);
        yield return new("Twin Blade", 24, 230, "Equipment - Weapons F440/G440, E440=50; one double weapon", Double: true);
        yield return new("Staff", 24, 270, "Equipment - Weapons F370/G370, E370=50; one two-handed QuarterStaff50");
    }
}
