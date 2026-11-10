using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Tests.Support;
using static SWLOR.Game.Server.Composition.GameServices;
using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.Tests.Perks;

public class SaberstaffAreaEffectsTests
{
    // The code under test reaches converted services through their static facades, which resolve from a container.
    private FacadeContainer _facades = null!;
    private IDisposable _facadeScope = null!;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void InstallFacades() => _facadeScope = _facades.Install();

    private const uint Creature = 61241;
    private static Dictionary<uint, CreatureStatusEffect> Effects =>
        StatusEffectInternals.CreatureEffects;

    [TearDown]
    public void Cleanup()
    {
        Effects.Remove(Creature);
        _facadeScope.Dispose();
    }

    [Test]
    public void TempestStance_AdjustsAreaAndSingleTargetDamageAcrossSkills()
    {
        var effects = new CreatureStatusEffect();
        effects.Add(new TempestStanceStatusEffect());
        Effects[Creature] = effects;
        foreach (var skill in new[] { SkillType.Saberstaff, SkillType.Force, SkillType.TwinBlade, SkillType.Mimicry })
        {
            var area = new AbilityDetail { SkillType = skill, IsHostileAbility = true, IsAreaAbility = true };
            var single = new AbilityDetail { SkillType = skill, IsHostileAbility = true, IsSingleTargetAbility = true };
            GameServices.CombatDamageModifiers.ApplyAbilityShapeDamageModifier(Creature, 100, true, area).Should().Be(110);
            GameServices.CombatDamageModifiers.ApplyAbilityShapeDamageModifier(Creature, 100, true, single).Should().Be(90);
        }
        GameServices.CombatDamageModifiers.ApplyAbilityShapeDamageModifier(Creature, 100, false, null).Should().Be(90);
        GameServices.CombatDamageModifiers.ApplyAbilityShapeDamageModifier(Creature, 100, true,
            new AbilityDetail { IsAreaAbility = true }).Should().Be(100, "friendly areas must not gain hostile damage modifiers");
        GameServices.CombatDamageModifiers.ApplyAbilityShapeDamageModifier(Creature, 0, true,
            new AbilityDetail { IsHostileAbility = true, IsAreaAbility = true }).Should().Be(0);
    }

    [Test]
    public void AreaUseBuffs_KeepTheirDurationWhenSeveralSourcesStack()
    {
        Stat.GetStatTypeAggregation(StatType.AreaAbilityUsedAttackDeflectionDurationSeconds)
            .Should().Be(StatTypeAggregation.Maximum);
        Stat.AggregateStatAdjustment(StatType.AreaAbilityUsedAttackDeflectionDurationSeconds, 30, 30)
            .Should().Be(30, "Spinning Deflection, Tempest Stance, and Saber Cyclone share a 30-second window");
    }

    [Test]
    public void AreaPulseSequence_AllowsOnlyOneTriggerAcrossDelayedImpacts()
    {
        var sequence = new AbilityImpactSequence();
        sequence.TryTriggerAreaPulse().Should().BeTrue();
        sequence.TryTriggerAreaPulse().Should().BeFalse();
        new AbilityImpactSequence().TryTriggerAreaPulse().Should().BeTrue();
    }
}
