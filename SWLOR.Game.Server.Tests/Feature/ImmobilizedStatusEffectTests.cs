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
        source.Should().Contain("ApplyEffectToObject(DurationType.Permanent, effect, creature);",
            "the tracked status must own the native root lifetime through scheduler grace, refreshes, and removal");
        source.Should().NotContain("StatType.MovementSpeedDisabled");
    }

    [Test]
    public void MovementSpeedDisabled_IsNonBeneficialStatType()
    {
        Stat.GetStatTypeCategory(StatType.MovementSpeedDisabled).Should().Be(StatTypeCategory.NonBeneficial);
    }

    [Test]
    public void ActivityCleanup_DoesNotRemoveTrackedImmobilizeRoots()
    {
        var root = FindRepositoryRoot();
        var server = Path.Combine(root, "SWLOR.Game.Server");
        var holoCom = File.ReadAllText(Path.Combine(server, "Service", "HoloCom.cs"));
        var craft = File.ReadAllText(Path.Combine(
            server,
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "CraftViewModel.cs"));
        var temporaryEffects = File.ReadAllText(Path.Combine(server, "Feature", "PlayerTemporaryEffects.cs"));

        holoCom.Should().Contain(
            "PlayerActivityEffectTag.HoloComImmobilize);");
        holoCom.Should().Contain("RemoveEffectByTag(sender, PlayerActivityEffectTag.HoloComImmobilize);");
        holoCom.Should().Contain("RemoveEffectByTag(receiver, PlayerActivityEffectTag.HoloComImmobilize);");
        holoCom.Should().NotContain("effectType == EffectTypeScript.CutsceneImmobilize");

        craft.Should().Contain(
            "var effect = TagEffect(EffectCutsceneImmobilize(), PlayerActivityEffectTag.CraftingImmobilize);");
        craft.Should().Contain("RemoveEffectByTag(Player, PlayerActivityEffectTag.CraftingImmobilize);");
        craft.Should().NotContain("GetEffectType(effect) == EffectTypeScript.CutsceneImmobilize");

        temporaryEffects.Should().Contain("RemoveStaleActivityImmobility(player);");
        temporaryEffects.Should().Contain(
            "RemoveEffectByTag(player, PlayerActivityEffectTag.CraftingImmobilize);");
        temporaryEffects.Should().Contain(
            "RemoveEffectByTag(player, PlayerActivityEffectTag.HoloComImmobilize);");
        temporaryEffects.Should().Contain(
            "RemoveEffectByTag(player, PlayerActivityEffectTag.RefiningImmobilize);");
        temporaryEffects.Should().Contain("string.IsNullOrWhiteSpace(GetEffectTag(effect))",
            "login cleanup may remove legacy untagged activity roots but must preserve tagged status roots");
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
