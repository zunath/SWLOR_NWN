using System.Collections;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.TelegraphService;

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
