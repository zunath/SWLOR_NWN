using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class AbilityAreaVisualEffectTests
{
    [Test]
    public void AreaCombatImpactVisualEffects_DoNotRequireHitTargets()
    {
        var root = FindRepositoryRoot();
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
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "AbilityDefinition",
            "CombatAreaPulses.cs"));

        source.Should().Contain("bool alwaysApplyAreaVisualEffect = true");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
