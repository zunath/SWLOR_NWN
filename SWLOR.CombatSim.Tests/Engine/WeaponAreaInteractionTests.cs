using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Engine;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;
using SWLOR.CombatSim.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.CombatSim.Tests.Engine;

[TestFixture]
public class WeaponAreaInteractionTests
{
    private AbilityCatalog _catalog;
    [OneTimeSetUp] public void Load() => _catalog = AbilityCatalog.Load();

    private Simulation Scenario(string feat, params string[] perks)
    {
        var owner = new CombatantSpec
        {
            Name = "owner", MaxHp = 100000, MaxStm = 100, ReactionSeconds = 100,
            WeaponCombat = new() { Accuracy = 10000, EnableCriticals = false }, Perks = perks.ToList(),
            Abilities = new() { new() { Feat = feat, Target = AbilityTarget.Self } }
        };
        var enemies = Enumerable.Range(1, 6).Select(index => new CombatantSpec
        {
            Name = "enemy" + index, MaxHp = 100000, MaxStm = 100, WeaponCombat = new() { PositionX = index }
        });
        var sim = TestScenarios.Spawn(TestScenarios.TwoTeams(true, new[] { owner }, enemies), RuleSets.Current(), _catalog);
        sim.Get("owner").NextActionAt = 0;
        return sim;
    }

    [TestCase(FeatType.SpinningWhirl3, 3, 30)]
    [TestCase(FeatType.InterruptingSweep2, 0, 30)]
    [TestCase(FeatType.ScrapheapLockdown1, 5, 25)]
    public void CatalogReadsSourceSphereCapAndPower(FeatType feat, int cap, int power)
    {
        var info = _catalog.Get(feat);
        info.IsAreaAbility.Should().BeTrue();
        info.WeaponAreaRadius.Should().Be(5);
        info.MaximumAreaTargets.Should().Be(cap);
        info.SourceWeaponPower.Should().Be(power);
    }

    [TestCase("SpinningWhirl3", 3)]
    [TestCase("InterruptingSweep2", 5)]
    [TestCase("ScrapheapLockdown1", 5)]
    public void StaticAreaRespectsRadiusAndAuthoredTargetCap(string feat, int expectedTargets)
    {
        var sim = Scenario(feat);
        SimulationClock.RunUntil(sim, 3);
        sim.Get("owner").Weapon.Diagnostics["AbilityImpactsAttempted"].Should().Be(expectedTargets);
        sim.Get("enemy6").Stats.DamageTakenToHp.Should().Be(0);
        if (expectedTargets == 3) sim.Get("enemy4").Stats.DamageTakenToHp.Should().Be(0);
    }

    [Test] public void StaticAreaRewardsCountDistinctHitsAndRefundOnlyOncePerCast()
    {
        var sim = Scenario("SpinningWhirl3", "Momentum", "SpinningRhythm:3", "FlowingFootwork", "SweepingAdvance", "RestorationStrike");
        SimulationClock.RunUntil(sim, 1);
        var owner = sim.Get("owner"); var state = owner.Weapon;
        state.Diagnostics["AreaTargetsHit"].Should().Be(3);
        state.Diagnostics.GetValueOrDefault("SweepCasts").Should().Be(0);
        state.AreaHaste(1).Should().Be(13);
        state.EvasionPercent(1).Should().Be(10);
        state.Diagnostics["CostlyHitStaminaRefunded"].Should().Be(3);
        state.Diagnostics["AreaStaminaRefunded"].Should().Be(6);
        owner.Stats.StmRefunded.Should().Be(9);
    }

    [Test] public void UnsupportedWeaponAreaShapeFailsInsteadOfApproximatingIt()
    {
        var original = _catalog.Get(FeatType.SpinningWhirl3);
        try
        {
            _catalog.Add(new AbilityInfo { Feat = original.Feat, Name = original.Name, RecastGroup = original.RecastGroup,
                IsHostile = true, IsAreaAbility = true, SourceWeaponPower = 30, STM = 10, SkillType = original.SkillType });
            var sim = Scenario("SpinningWhirl3");
            var act = () => SimulationClock.RunUntil(sim, 1);
            act.Should().Throw<InvalidDataException>().WithMessage("*self-centered sphere*");
        }
        finally { _catalog.Add(original); }
    }

    private Simulation Calibrated(string feat, DamageSpec damage, params string[] perks)
    {
        var info = _catalog.Get(Enum.Parse<FeatType>(feat));
        var owner = new CombatantSpec
        {
            Name = "owner", MaxHp = 100000, MaxStm = 100, ReactionSeconds = 100,
            WeaponCombat = new(), Perks = perks.ToList(),
            Abilities = new() { new() { Feat = feat, Damage = damage,
                Target = info.IsAimed ? AbilityTarget.Enemy : AbilityTarget.Self } }
        };
        var enemies = Enumerable.Range(1, 3).Select(index => new CombatantSpec
        {
            Name = "enemy" + index, MaxHp = 100000, MaxStm = 100, WeaponCombat = new()
        });
        var sim = TestScenarios.Spawn(TestScenarios.TwoTeams(false, new[] { owner }, enemies), RuleSets.Current(), _catalog);
        sim.Get("owner").NextActionAt = 0;
        return sim;
    }

    [TestCase("CrossCut4", 1f, 3)]
    [TestCase("CrossCut4", 0f, 0)]
    [TestCase("JointLock3", 1f, 3)]
    public void CalibratedHitsApplyRestorationOnceRegardlessOfDamageImplementation(string feat, float hitChance, int expected)
    {
        var sim = Calibrated(feat, new() { Min = 5, Max = 5, Hits = 2, HitChance = hitChance }, "RestorationStrike");
        SimulationClock.RunUntil(sim, 1);
        sim.Get("owner").Stats.StmRefunded.Should().Be(expected);
        sim.Get("owner").Weapon.Diagnostics.GetValueOrDefault("CostlyHitStaminaRefunded").Should().Be(expected);
    }

    [TestCase(1, 2, 9, 7)]
    [TestCase(3, 2, 9, 7)]
    [TestCase(3, 0, 0, 0)]
    public void CalibratedAreaCountsDistinctSuccessfulVictimsAndSharesRefundBudget(int hits, int landedTargets, int haste, int refund)
    {
        var damage = new DamageSpec { Min = 5, Max = 5, Hits = hits, Area = true,
            HitChance = landedTargets == 0 ? 0 : 1, HitChanceVs = new() { ["enemy3"] = 0 } };
        var sim = Calibrated("SpinningWhirl3", damage,
            "RestorationStrike", "Momentum", "SpinningRhythm:3", "FlowingFootwork", "SweepingAdvance");
        SimulationClock.RunUntil(sim, 1);
        var owner = sim.Get("owner");
        owner.Weapon.Diagnostics["AreaCasts"].Should().Be(1);
        owner.Weapon.Diagnostics["AreaTargetsHit"].Should().Be(landedTargets);
        owner.Weapon.AreaHaste(1).Should().Be(haste);
        owner.Weapon.EvasionPercent(1).Should().Be(10, "the area-use reward does not require a hit");
        owner.Stats.StmRefunded.Should().Be(refund);
        owner.Stats.StmRefunded.Should().BeLessThan(owner.Stats.StmSpent);
    }
}
