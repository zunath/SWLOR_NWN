using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Tests.Feature;

public class ImmobilizedStatusEffectTests
{
    [Test]
    public void ImmobilizedStatusEffect_UsesTheLegOnlyNativeRoot()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "SWLOR.Game.Server",
            "Feature",
            "StatusEffectDefinition",
            "ImmobilizedStatusEffect.cs"));

        source.Should().Contain("TagNativeEffect(EffectCutsceneImmobilize())",
            "a zero movement-rate factor still permits visible creeping in the client");
        source.Should().NotContain("StatType.MovementSpeedDisabled");
    }

    [Test]
    public void MovementSpeedDisabled_IsNonBeneficialStatType()
    {
        Stat.GetStatTypeCategory(StatType.MovementSpeedDisabled).Should().Be(StatTypeCategory.NonBeneficial);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
