using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class NativeControlStatusEffectTests
{
    [Test]
    public void StatusEffectBase_CentralizesNativeEffectTags()
    {
        var root = FindRepositoryRoot();
        var statusEffectBase = File.ReadAllText(Path.Combine(
            root,
            "SWLOR.Game.Server",
            "Service",
            "StatusEffectService",
            "StatusEffectBase.cs"));
        var statusEffectService = File.ReadAllText(Path.Combine(
            root,
            "SWLOR.Game.Server",
            "Service",
            "StatusEffect.cs"));

        statusEffectBase.Should().Contain("protected Effect TagNativeEffect");
        statusEffectBase.Should().Contain("void RemoveNativeEffects");
        statusEffectBase.Should().Contain(":Native:");
        statusEffectService.Should().Contain("statusEffect.RemoveNativeEffects(creature);");
    }

    [TestCase("BlindStatusEffect.cs", "EffectBlindness()")]
    [TestCase("DazedStatusEffect.cs", "EffectDazed()")]
    [TestCase("KnockdownStatusEffect.cs", "EffectKnockdown()")]
    [TestCase("StunnedStatusEffect.cs", "EffectStunned()")]
    [TestCase("TranquilizedStatusEffect.cs", "IgnoreEffectImmunity(EffectSleep())")]
    public void NativeControlStatusEffects_UseCentralNativeEffectTagging(
        string fileName,
        string nativeEffect)
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "SWLOR.Game.Server",
            "Feature",
            "StatusEffectDefinition",
            fileName));

        source.Should().Contain($"TagNativeEffect({nativeEffect})");
        source.Should().NotContain($"TagEffect({nativeEffect}, Id)");
    }

    [Test]
    public void StatusEffectDefinitions_DoNotTagNativeEffectsWithTrackerIds()
    {
        var root = FindRepositoryRoot();
        var statusEffectDirectory = Path.Combine(
            root,
            "SWLOR.Game.Server",
            "Feature",
            "StatusEffectDefinition");

        foreach (var file in Directory.GetFiles(statusEffectDirectory, "*StatusEffect.cs"))
        {
            var source = File.ReadAllText(file);
            source.Should().NotMatchRegex(@"TagEffect\s*\([^;\r\n]+,\s*Id\s*\)");
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            var candidate = directory.FullName;
            if (File.Exists(Path.Combine(candidate, "SWLOR.Game.Server.sln")) &&
                Directory.Exists(Path.Combine(candidate, "SWLOR.Game.Server")))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
