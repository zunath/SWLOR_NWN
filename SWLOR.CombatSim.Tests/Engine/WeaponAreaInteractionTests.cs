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
}
