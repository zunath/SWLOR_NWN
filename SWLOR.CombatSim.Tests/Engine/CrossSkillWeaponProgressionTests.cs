using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Engine;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;
using SWLOR.CombatSim.Tests.Support;
using SWLOR.Game.Server.Service;

namespace SWLOR.CombatSim.Tests.Engine;

[TestFixture]
public class CrossSkillWeaponProgressionTests
{
    [Test]
    public void TitanPairedPistols_ReportCadenceAgainstAnEqualPointMatchedRhythm()
    {
        const int samples = 64;
        var catalog = AbilityCatalog.Load();
        var gear = Gear().Single(profile => profile.Rank == 10 && profile.Paired);
        var rows = new List<object>();
        foreach (var haste in new[] { 0, 50 })
        foreach (var alternative in new[] { "MatchedRhythm:1", "Cadence" })
        {
            var actors = new List<Combatant>();
            foreach (var seed in Enumerable.Range(20261018, samples))
            {
                var scenario = Create(gear, "PairedVolley", false, haste, 60, catalog);
                scenario.Teams[0].Members[0].Perks.Add(alternative);
                scenario.Teams[0].Members[0].Abilities.Add(new() { Feat = Ability(gear.Rank, "ClosingShot") });
                catalog.Perks.GetPurchaseCost(scenario.Teams[0].Members[0].Perks).Should().Be(8,
                    "Cadence and Matched Rhythm I each cost2SP above the same6SP active purchases");
                var sim = TestScenarios.Spawn(scenario, RuleSets.Current(), catalog, seed);
                var actor = sim.Combatants[0]; actor.NextActionAt = actor.NextAttackAt = 0;
                for (var step = 0; step <= 1200; step++) { sim.Now = step * .05; sim.Step(.05); }
                actors.Add(actor);
            }
            var damage = actors.Select(actor => actor.Stats.DamageDealt).ToArray();
            rows.Add(new { gear = gear.Name, gear.Rank, gear.Source, haste, alternative, samples,
                seconds = 60, purchasedPerkPoints = 8, armorPoints = 9,
                damageMean = damage.Average(), damageMean95HalfWidth = HalfWidth(damage), dps = damage.Average() / 60,
                ordinaryDamage = actors.Average(actor => actor.Stats.OrdinaryDamage), abilityDamage = actors.Average(actor => actor.Stats.AbilityDamage),
                abilityUses = actors.Average(actor => actor.Stats.AbilitiesUsed),
                cadenceSpent = actors.Average(actor => actor.Stats.CadenceSpent), cadenceChains = actors.Average(actor => actor.Stats.CadenceChains),
                staminaPaid = actors.Average(actor => actor.Stats.StmSpent), staminaRefunded = actors.Average(actor => actor.Stats.StmRefunded) });
        }
        var directory = Environment.GetEnvironmentVariable("TWIN_SIM_OUTPUT") ?? Path.Combine(Path.GetTempPath(), "swlor-twin-cross-skill-sim-20261018");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "cross-skill-titan-matched-points.json");
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            method = "Four cells with the same64 seeds and exact Titan paired gear/ratings/60s clock as cross-skill-low-tier-cadence.json. " +
                "Either Cadence(2SP) or Matched RhythmI(2SP,+3%Haste) completes8 total active/trait SP with explicit9ArmorSP for offhand-delay30%. " +
                "Both use VolleyI then Closing ShotI immediately/on cooldown; Cadence comes only from a different paid ability landing within six seconds. " +
                "All same-rank neutral ratings, upper attributes26, finite61STM/20STMper30s, seed order and native rounding remain unchanged; no additional perks, points or unmodeled procs. " +
                "External50 haste is a capped sensitivity, not a standalone rank10 equipment claim.",
            rows
        }, new JsonSerializerOptions { WriteIndented = true }));
        TestContext.AddTestAttachment(path);
        TestContext.Progress.WriteLine($"Titan matched-points: {rows.Count} cells x{samples} seeds; {path}");
    }

    [Test]
    public void LowTierCadence_ReportsPrechargedBurstAndChainedRotation()
    {
        const int samples = 64;
        var catalog = AbilityCatalog.Load();
        var burstRows = new List<object>();
        var preparationRows = new List<object>();
        foreach (var gear in Gear())
        {
            foreach (var ability in new[] { "PairedVolley", "FocusedBurst", "ClosingShot" })
            foreach (var cadence in new[] { 0, 3 })
            {
                var actors = new List<Combatant>();
                foreach (var seed in Enumerable.Range(20261018, samples))
                {
                    var scenario = Create(gear, ability, true, 0, 5, catalog);
                    scenario.Teams[0].Members[0].Abilities[0].MinIntervalSeconds = 600;
                    var sim = TestScenarios.Spawn(scenario, RuleSets.Current(), catalog, seed);
                    var actor = sim.Combatants[0]; actor.NextActionAt = 0; actor.NextAttackAt = double.PositiveInfinity;
                    sim.GrantCadence(actor, cadence);
                    SimulationClock.RunUntil(sim, () => actor.Stats.AbilitiesUsed == 1 && actor.Casting == null, 5, "native low-tier activation");
                    // Twin Pistol builders keep Cadence for the spenders, so only a Cadence-spending ability applies it.
                    var spendsCadence = !catalog.Get(Enum.Parse<SWLOR.NWN.API.NWScript.Enum.FeatType>(Ability(gear.Rank, ability))).KeepsWeaponAbilityCadence;
                    actor.Stats.CadenceDamagePowerApplied.Should().Be(spendsCadence ? cadence * 2 * gear.Weapon(0).DistinctAttackItemCount : 0);
                    actor.Stats.StmRefunded.Should().BeLessThan(actor.Stats.StmSpent);
                    actors.Add(actor);
                }
                var damage = actors.Select(actor => actor.Stats.AbilityDamage).ToArray();
                burstRows.Add(new { gear = gear.Name, gear.Rank, gear.Source, ability = Ability(gear.Rank, ability), cadence, samples,
                    damageMean = damage.Average(), damageMean95HalfWidth = HalfWidth(damage),
                    cadenceRawDamageBudget = actors.Average(actor => actor.Stats.CadenceDamagePowerApplied),
                    staminaPaid = actors.Average(actor => actor.Stats.StmSpent), staminaRefunded = actors.Average(actor => actor.Stats.StmRefunded) });
            }

            foreach (var haste in new[] { 0, 50 })
            foreach (var ownsCadence in new[] { false, true })
            {
                var actors = new List<Combatant>();
                var firstCast = new List<double>();
                foreach (var seed in Enumerable.Range(20261018, samples))
                {
                    var scenario = Create(gear, "PairedVolley", ownsCadence, haste, 60, catalog);
                    scenario.Teams[0].Members[0].Abilities.Add(new() { Feat = Ability(gear.Rank, "ClosingShot") });
                    var sim = TestScenarios.Spawn(scenario, RuleSets.Current(), catalog, seed);
                    var actor = sim.Combatants[0]; actor.NextActionAt = actor.NextAttackAt = 0;
                    var observedFirst = false;
                    for (var step = 0; step <= 1200; step++)
                    {
                        sim.Now = step * .05;
                        sim.Step(.05);
                        if (!observedFirst && actor.Stats.AbilitiesUsed > 0)
                        {
                            observedFirst = true;
                            firstCast.Add(sim.Now);
                        }
                    }
                    observedFirst.Should().BeTrue();
                    actor.Stats.StmSpent.Should().BeGreaterThanOrEqualTo(actor.Stats.StmRefunded);
                    if (!ownsCadence) actor.Stats.CadenceSpent.Should().Be(0, "no charges exist without the Cadence trait");
                    actors.Add(actor);
                }
                var damage = actors.Select(actor => actor.Stats.DamageDealt).ToArray();
                preparationRows.Add(new { gear = gear.Name, gear.Rank, gear.Source, haste,
                    policy = ownsCadence ? "Volley then Closing Shot with Cadence" : "omit Cadence, same rotation", samples, seconds = 60,
                    weapon = gear.Weapon(haste), purchasedPerkPoints = catalog.Perks.GetPurchaseCost(Create(gear, "PairedVolley", ownsCadence, haste, 60, catalog).Teams[0].Members[0].Perks),
                    firstCastSeconds = firstCast.Average(),
                    damageMean = damage.Average(), damageMean95HalfWidth = HalfWidth(damage), dps = damage.Average() / 60,
                    ordinaryDamage = actors.Average(actor => actor.Stats.OrdinaryDamage), abilityDamage = actors.Average(actor => actor.Stats.AbilityDamage),
                    abilityUses = actors.Average(actor => actor.Stats.AbilitiesUsed), cadenceSpent = actors.Average(actor => actor.Stats.CadenceSpent),
                    cadenceChains = actors.Average(actor => actor.Stats.CadenceChains),
                    staminaPaid = actors.Average(actor => actor.Stats.StmSpent), staminaRefunded = actors.Average(actor => actor.Stats.StmRefunded) });
            }
        }
        var directory = Environment.GetEnvironmentVariable("TWIN_SIM_OUTPUT") ?? Path.Combine(Path.GetTempPath(), "swlor-twin-cross-skill-sim-20261018");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "cross-skill-low-tier-cadence.json");
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            method = "64 fixed seeds per cell,8 actual tier/loadout profiles: Titan(rank10)/Proto(rank30), solo Doublehand Pistol/paired Pistol/Great Sword/Rifle. " +
                "DMG/Delay from exact Bible rows; item ATT/ACC0. Attribute26 is the legal upper-attribute control; native Stat formulas derive same-rank ATT/ACC/DEF/EVA and ordinary weapon rank follows gear rank. " +
                "These are neutral comparisons, not authored NPCs or full character builds. Explicit9ArmorSP for DoublehandIII or paired offhand delay30%; two-handed profiles have neither. " +
                "Precharged0/3 Cadence burst compares legal Volley/Focused ranks with the Closing Shot spender at the same initial charges. " +
                "The60s rotation starts with0 Cadence and no discount and uses Volley then Closing Shot on cooldown; Cadence comes only from a different paid ability landing within six seconds. " +
                "Cast delay, retained auto cooldown, native haste and all available ordinary damage are charged. " +
                "Omission removes only the2SP Cadence perk and retains all other purchases; points are not redistributed. Rank10 cannot buy Follow Through; rank30 purchases it in both controls to isolate Cadence. " +
                "External haste0/50 is sensitivity, not a claim of a standalone low-rank build.61STM,20STM/30s. No item procs, shields, terrain, other skills' passive families or global balance proof.",
            burstCells = burstRows.Count, preparationCells = preparationRows.Count, samplesPerCell = samples,
            burstRows, preparationRows
        }, new JsonSerializerOptions { WriteIndented = true }));
        TestContext.AddTestAttachment(path);
        TestContext.Progress.WriteLine($"Low-tier Cadence: {burstRows.Count + preparationRows.Count} cells x{samples} seeds; {path}");
    }

    private static ScenarioSpec Create(TierGear gear, string prefix, bool cadence, int haste, int seconds, AbilityCatalog catalog)
    {
        var scenario = TwinPistolScenarios.Create(false, true, seconds, gear.Rank, catalog: catalog);
        var spec = scenario.Teams[0].Members[0];
        spec.Perks = new() { gear.Rank == 10 ? "PairedVolley:1" : "PairedVolley:3",
            gear.Rank == 10 ? "FocusedBurst:1" : "FocusedBurst:2", gear.Rank == 10 ? "ClosingShot:1" : "ClosingShot:2" };
        if (cadence) spec.Perks.Add("Cadence");
        if (gear.Rank >= 25) spec.Perks.Add("TwinPistolFollowThrough");
        spec.WeaponLoadout = gear.Weapon(haste);
        spec.Abilities = new() { new() { Feat = Ability(gear.Rank, prefix) } };
        var target = scenario.Teams[1].Members[0];
        target.Defense = Stat.CalculateDefense(26, gear.Rank, 0); target.Evasion = Stat.GetEvasion(gear.Rank, 26, 0);
        return scenario;
    }

    private static string Ability(int rank, string prefix) => prefix + (rank == 10 ? "1" : prefix == "PairedVolley" ? "3" : "2");
    private static double HalfWidth(double[] values)
    {
        var mean = values.Average();
        return 1.96 * Math.Sqrt(values.Sum(value => Math.Pow(value - mean, 2)) / (values.Length - 1)) / Math.Sqrt(values.Length);
    }

    private sealed record TierGear(string Name, int Rank, int Damage, int Delay, string Source, bool Single = false, bool Paired = false)
    {
        public WeaponLoadoutSpec Weapon(int haste) => new()
        {
            MainDamage = Damage, MainDelay = Delay, OffhandDamage = Paired ? Damage : 0, OffhandDelay = Paired ? Delay : 0,
            SingleWeaponDamagePercent = Single ? 60 : 0, OffhandDelayReductionPercent = Paired ? 30 : 0,
            SkillRank = Rank, WeaponRank = Rank, Attack = Stat.GetAttack(Rank, 26, 0), Accuracy = Stat.GetAccuracy(Rank, 26, 0), HastePercent = haste
        };
    }

    private static IEnumerable<TierGear> Gear()
    {
        yield return new("Titan solo Pistol / DoublehandIII", 10, 9, 250, "Equipment - Weapons E184/F184/G184/I184/J184", Single: true);
        yield return new("Titan paired Pistols", 10, 9, 250, "Equipment - Weapons E184/F184/G184/I184/J184 x2", Paired: true);
        yield return new("Titan Great Sword", 10, 15, 300, "Equipment - Weapons E10/F10/G10/I10/J10");
        yield return new("Titan Rifle", 10, 14, 300, "Equipment - Weapons E222/F222/G222/I222/J222");
        yield return new("Proto solo Pistol / DoublehandIII", 30, 16, 250, "Equipment - Weapons E195/F195/G195/I195/J195", Single: true);
        yield return new("Proto paired Pistols", 30, 16, 250, "Equipment - Weapons E195/F195/G195/I195/J195 x2", Paired: true);
        yield return new("Proto Great Sword", 30, 34, 300, "Equipment - Weapons E20/F20/G20/I20/J20");
        yield return new("Proto Rifle", 30, 31, 300, "Equipment - Weapons E233/F233/G233/I233/J233");
    }
}
