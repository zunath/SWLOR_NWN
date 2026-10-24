using System.Collections;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Vibroblade;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.TelegraphService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class AbilityImpactShapeTests
{
    private const uint Activator = 0x7F000101;

    [Test]
    public void SingleTargetAbility_RecordingTwoTargets_KeepsAuthoredShape()
    {
        var ability = new AbilityDetail { IsHostileAbility = true, IsSingleTargetAbility = true };
        WithTrackedImpact(ability, impact =>
        {
            Invoke(impact, "RecordTarget", 11u);
            Invoke(impact, "RecordTarget", 12u);

            Ability.IsSingleTargetImpact(Activator, ability).Should().BeTrue();
            Ability.IsAreaImpact(Activator, ability).Should().BeFalse();
        });
    }

    [Test]
    public void UnflaggedAbility_RecordingSingleTargetShape_GetsNoShapeAdjustments()
    {
        var ability = new AbilityDetail { IsHostileAbility = true };
        WithTrackedImpact(ability, impact =>
        {
            Invoke(impact, "RecordShape", SkillType.Invalid, false);
            Invoke(impact, "RecordTarget", 11u);

            Ability.IsSingleTargetImpact(Activator, ability).Should().BeFalse();
            Ability.IsAreaImpact(Activator, ability).Should().BeFalse();
        });
    }

    [Test]
    public void MeleeTechnique_RecordingTwoTargets_IsAreaForThatCast()
    {
        var ability = new AbilityDetail
        {
            IsHostileAbility = true,
            IsSingleTargetAbility = true,
            MeleeTechnique = new MeleeTechniqueDetail()
        };
        WithTrackedImpact(ability, impact =>
        {
            Invoke(impact, "RecordTarget", 11u);
            Invoke(impact, "RecordTarget", 12u);

            Ability.IsAreaImpact(Activator, ability).Should().BeTrue();
            Ability.IsSingleTargetImpact(Activator, ability).Should().BeFalse();
        });
    }

    [TestCase(typeof(SavageCleaveAbilityDefinition), FeatType.SavageCleave1)]
    [TestCase(typeof(SavageCleaveAbilityDefinition), FeatType.SavageCleave2)]
    [TestCase(typeof(SavageCleaveAbilityDefinition), FeatType.SavageCleave3)]
    [TestCase(typeof(CoveringStrikeAbilityDefinition), FeatType.CoveringStrike1)]
    [TestCase(typeof(CoveringStrikeAbilityDefinition), FeatType.CoveringStrike2)]
    [TestCase(typeof(CoveringStrikeAbilityDefinition), FeatType.CoveringStrike3)]
    public void SelfCenteredVibrobladeSweeps_CountAsAreaAbilities(Type definitionType, FeatType feat)
    {
        // Spinning Deflection, Tempest Stance, and every other "after using an area combat
        // ability" reward read this shape; an unflagged sweep silently earns none of them.
        var ability = ((IAbilityListDefinition)System.Activator.CreateInstance(definitionType)!).BuildAbilities()[feat];

        WithTrackedImpact(ability, impact =>
        {
            Invoke(impact, "RecordShape", SkillType.Vibroblade, true);
            Invoke(impact, "RecordTarget", 11u);

            Ability.IsAreaImpact(Activator, ability).Should().BeTrue();
            Ability.IsSingleTargetImpact(Activator, ability).Should().BeFalse();
        });
    }

    [Test]
    public void HostileAbilities_DeclareAnAreaOrSingleTargetShape()
    {
        // An unflagged hostile ability is neither area nor single target, so it silently skips every
        // shape-gated perk (Spinning Deflection, area and single-target damage adjustments, and more).
        var offenders = typeof(IAbilityListDefinition).Assembly
            .GetTypes()
            .Where(x => typeof(IAbilityListDefinition).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
            .SelectMany(x => ((IAbilityListDefinition)System.Activator.CreateInstance(x)!).BuildAbilities()
                .Where(a => a.Value.IsHostileAbility &&
                            !a.Value.IsAreaAbility &&
                            !a.Value.IsSingleTargetAbility)
                .Select(a => $"{x.Name}.{a.Key}"))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        offenders.Should().BeEmpty("every hostile ability must call IsAreaAbility() or IsSingleTargetAbility()");
    }

    private static void WithTrackedImpact(AbilityDetail ability, Action<object> assert)
    {
        var impactType = typeof(Ability).GetNestedType("TrackedAbilityImpact", BindingFlags.NonPublic)!;
        var impact = impactType.GetConstructors().Single().Invoke(new object[]
        {
            ability, 0, 0, 0, 0, 0, false, 0, Array.Empty<TelegraphGeometry>(), new AbilityImpactSequence()
        });
        var impacts = (IDictionary)typeof(Ability)
            .GetField("_trackedAbilityImpacts", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null)!;
        impacts[Activator] = impact;
        try
        {
            assert(impact);
        }
        finally
        {
            impacts.Remove(Activator);
        }
    }

    private static void Invoke(object impact, string method, params object[] arguments) =>
        impact.GetType().GetMethod(method)!.Invoke(impact, arguments);
}
