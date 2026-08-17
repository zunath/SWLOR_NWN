using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class PistolAnimationRemapTests
{
    [Test]
    public void RemapsTheSlingAttackToTheFormerPistolAttack()
    {
        PistolAnimationRemap.SlingAttackAnimation.Should().Be("throwr");
        PistolAnimationRemap.FormerPistolAttackAnimation.Should().Be("bowshot");
    }

    [TestCase(BaseItem.Pistol)]
    [TestCase(BaseItem.LegacyPistol)]
    [TestCase(BaseItem.Sling)]
    public void UnshieldedPistols_UseTheFormerAttackAnimation(BaseItem pistolBaseItem)
    {
        var result = PistolAnimationRemap.ShouldUseFormerPistolAttackAnimation(
            pistolBaseItem,
            null);

        result.Should().BeTrue();
    }

    [TestCase(BaseItem.Pistol, BaseItem.SmallShield)]
    [TestCase(BaseItem.LegacyPistol, BaseItem.LargeShield)]
    [TestCase(BaseItem.Sling, BaseItem.TowerShield)]
    public void ShieldedPistols_KeepTheSlingAttackAnimation(
        BaseItem pistolBaseItem,
        BaseItem shieldBaseItem)
    {
        var result = PistolAnimationRemap.ShouldUseFormerPistolAttackAnimation(
            pistolBaseItem,
            shieldBaseItem);

        result.Should().BeFalse();
    }

    [Test]
    public void PistolsWithAnotherOffhandItem_KeepTheSlingAttackAnimation()
    {
        var result = PistolAnimationRemap.ShouldUseFormerPistolAttackAnimation(
            BaseItem.Pistol,
            BaseItem.Longsword);

        result.Should().BeFalse();
    }

    [TestCase(BaseItem.Longsword)]
    [TestCase(BaseItem.Rifle)]
    [TestCase(null)]
    public void NonPistolLoadouts_AreNotRemapped(BaseItem? rightHandBaseItem)
    {
        var result = PistolAnimationRemap.ShouldUseFormerPistolAttackAnimation(
            rightHandBaseItem,
            null);

        result.Should().BeFalse();
    }

    [TestCase(Animation.ThrowGrenade, true, true)]
    [TestCase(Animation.ThrowGrenade, false, false)]
    [TestCase(Animation.PointPistol, true, false)]
    [TestCase(Animation.Invalid, true, false)]
    public void ExplicitThrows_SuspendOnlyAnActivePistolRemap(
        Animation animation,
        bool isPistolRemapActive,
        bool expected)
    {
        var result = PistolAnimationRemap.ShouldSuspendForExplicitThrow(
            animation,
            isPistolRemapActive);

        result.Should().Be(expected);
    }

    [Test]
    public void ExplicitThrows_ArePreservedAcrossSharedAnimationPipelines()
    {
        var root = FindRepositoryRoot();
        var activationSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "UsePerkFeat.cs"));
        var impactSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Ability.cs"));

        activationSource.Should().Contain(
            "PistolAnimationRemap.PlayAnimationPreservingExplicitThrow",
            "activation animations must bypass the persistent pistol remap for explicit throws");
        impactSource.Should().Contain(
            "PistolAnimationRemap.PlayAnimationPreservingExplicitThrow",
            "impact animations must bypass the persistent pistol remap for explicit throws");
    }

    [TestCase(nameof(PistolAnimationRemap.OnClientEnter), ScriptName.OnModuleEnter)]
    [TestCase(nameof(PistolAnimationRemap.OnCreatureSpawn), ScriptName.OnCreatureSpawnAfter)]
    [TestCase(nameof(PistolAnimationRemap.OnPlayerRespawn), ScriptName.OnModuleRespawn)]
    [TestCase(nameof(PistolAnimationRemap.OnItemEquip), ScriptName.OnItemEquipValidateAfter)]
    [TestCase(nameof(PistolAnimationRemap.OnItemUnequip), ScriptName.OnItemUnequipAfter)]
    public void SynchronizesAtEveryEquipmentLifecycleBoundary(
        string methodName,
        string expectedScript)
    {
        var method = typeof(PistolAnimationRemap).GetMethod(methodName);
        var scripts = method!
            .GetCustomAttributes(typeof(NWNEventHandler), false)
            .Cast<NWNEventHandler>()
            .Select(attribute => attribute.Script);

        scripts.Should().Contain(expectedScript);
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
