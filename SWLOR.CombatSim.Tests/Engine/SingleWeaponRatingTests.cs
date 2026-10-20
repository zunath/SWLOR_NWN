using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Engine;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.CombatSim.Tests.Engine;

[TestFixture]
public class SingleWeaponRatingTests
{
    [TestCase("PairedVolley:4", "PairedVolley4", 20)]
    [TestCase("PairedVolley:4", "PairedVolley4", 60)]
    [TestCase("FocusedBurst:3", "FocusedBurst3", 20)]
    [TestCase("FocusedBurst:3", "FocusedBurst3", 60)]
    public void ConditionalRatingBonus_MatchesNativeEffectiveDmg_ForAutosAndAbilities(string perk, string feat, int percent)
    {
        var catalog = AbilityCatalog.Load();
        var conditional = Create(perk, feat, 23, percent);
        var effective = Create(perk, feat, WeaponDamage.CalculateEffectiveDMG(23, percent), 0);
        foreach (var seed in new[] { 1, 47, 20261018 })
        {
            var adjusted = new Simulation(conditional, RuleSets.Current(), catalog, seed).Run().Combatants[0];
            var reference = new Simulation(effective, RuleSets.Current(), catalog, seed).Run().Combatants[0];
            adjusted.Stats.OrdinaryDamage.Should().Be(reference.Stats.OrdinaryDamage,
                "Single Weapon modifies the rating before native damage ranges and rounding");
            adjusted.Stats.AbilityDamage.Should().Be(reference.Stats.AbilityDamage,
                "the same effective weapon rating is captured by activated weapon abilities");
            adjusted.Stats.AbilityDamage.Should().BeGreaterThan(0);
            adjusted.Stats.OrdinaryDamage.Should().BeGreaterThan(0);
        }
    }

    [Test]
    public void ConditionalSingleWeaponRating_CannotBeDeclaredWithAnAttackingOffhand()
    {
        var loadout = new WeaponLoadoutSpec { MainDamage = 23, OffhandDamage = 23, OffhandDelay = 250, SingleWeaponDamagePercent = 20 };
        Action validate = loadout.Validate;
        validate.Should().Throw<InvalidDataException>();
    }

    private static ScenarioSpec Create(string perk, string feat, int damage, int percent)
    {
        var scenario = TwinPistolScenarios.Create(false, true, duration: 40);
        var actor = scenario.Teams[0].Members[0];
        actor.Perks = new() { perk };
        actor.WeaponLoadout.MainDamage = damage;
        actor.WeaponLoadout.OffhandDamage = actor.WeaponLoadout.OffhandDelay = 0;
        actor.WeaponLoadout.SingleWeaponDamagePercent = percent;
        actor.Abilities = new() { new() { Feat = feat } };
        return scenario;
    }
}
