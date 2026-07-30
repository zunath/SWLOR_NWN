using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Feature;

public class AbilityAreaVisualEffectTests
{
    [Test]
    public void AreaCombatImpactVisualEffects_DoNotRequireHitTargets()
    {
        var root = RepoPaths.FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
                root.FullName,
                "SWLOR.Game.Server",
                "Service",
                "Ability.cs"))
            .Replace("\r\n", "\n");

        source.Should().Contain(
            "if (areaVisualEffect != VisualEffect.None &&\n                    GetIsObjectValid(GetAreaFromLocation(center)))");
        source.Should().NotContain(
            "if (creatures.Any(creature => GetIsObjectValid(creature) && GetIsReactionTypeHostile(creature, activator)))");
        source.Should().Contain("bool alwaysApplyAreaVisualEffect = true");
        source.Should().Contain("if (alwaysApplyAreaVisualEffect && areaVisualEffect != VisualEffect.None)");
    }

    [Test]
    public void CombatAreaPulses_DefaultAreaVisualEffectsToAlwaysApply()
    {
        var root = RepoPaths.FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "AbilityDefinition",
            "CombatAreaPulses.cs"));

        source.Should().Contain("bool alwaysApplyAreaVisualEffect = true");
    }

}
