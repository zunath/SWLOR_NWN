using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class PistolAnimationRemapTests
{
    /// <summary>
    /// Verifies the native sling carrier and former pistol replacement animation identifiers.
    /// </summary>
    [Test]
    public void RemapsTheSlingAttackToTheFormerPistolAttack()
    {
        PistolAnimationRemap.SlingAttackAnimation.Should().Be("throwr");
        PistolAnimationRemap.FormerPistolAttackAnimation.Should().Be("bowshot");
    }

    /// <summary>
    /// Verifies every pistol-compatible base item uses the former animation with an empty offhand.
    /// </summary>
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

    /// <summary>
    /// Verifies shielded pistol loadouts retain the sling-compatible animation.
    /// </summary>
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

    /// <summary>
    /// Verifies any non-shield offhand item also prevents the two-handed replacement animation.
    /// </summary>
    [Test]
    public void PistolsWithAnotherOffhandItem_KeepTheSlingAttackAnimation()
    {
        var result = PistolAnimationRemap.ShouldUseFormerPistolAttackAnimation(
            BaseItem.Pistol,
            BaseItem.Longsword);

        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies non-pistol loadouts are never remapped.
    /// </summary>
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

    /// <summary>
    /// Verifies only explicit throws with an active pistol remap require suspension.
    /// </summary>
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

    /// <summary>
    /// Verifies fallback and configured activation and impact paths preserve explicit throws.
    /// </summary>
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
            "fallback activation animations must bypass the persistent pistol remap for explicit throws");
        System.Text.RegularExpressions.Regex.IsMatch(
            activationSource,
            @"PlayAnimationWithTemporaryReplacementPreservingExplicitThrow\s*\(\s*activator,\s*ability\.AnimationType,\s*1\.0f,\s*animationLength,\s*sourceAnimationName,\s*replacementAnimationName,\s*ability\.AnimationRestoreDelaySeconds\s*\)")
            .Should().BeTrue(
                "configured activation replacements must bypass the persistent pistol remap for explicit throws");
        impactSource.Should().Contain(
            "PistolAnimationRemap.PlayAnimationPreservingExplicitThrow",
            "fallback impact animations must bypass the persistent pistol remap for explicit throws");
        System.Text.RegularExpressions.Regex.IsMatch(
            impactSource,
            @"PlayAnimationWithTemporaryReplacementPreservingExplicitThrow\s*\(\s*activator,\s*animation,\s*1\.0f,\s*restoreDelaySeconds,\s*sourceAnimationName,\s*replacementAnimationName,\s*restoreDelaySeconds\s*\)")
            .Should().BeTrue(
                "configured impact replacements must bypass the persistent pistol remap for explicit throws");
    }

    /// <summary>
    /// Verifies the remap synchronizes at every equipment and creature lifecycle boundary.
    /// </summary>
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

    /// <summary>
    /// Verifies persistent lifecycle boundaries discard stale throw state before forcing a remap.
    /// </summary>
    [Test]
    public void PersistentLifecycleBoundaries_ResetTransientThrowSuspension()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "PistolAnimationRemap.cs"));

        source.Should().Contain(
            "ResetTransientSuspensionAndSyncAnimationState(GetEnteringObject());");
        source.Should().Contain(
            "ResetTransientSuspensionAndSyncAnimationState(OBJECT_SELF);");
        source.Should().Contain(
            "ResetTransientSuspensionAndSyncAnimationState(GetLastRespawnButtonPresser());");
        System.Text.RegularExpressions.Regex.IsMatch(
                source,
                @"DeleteLocalInt\(creature, ExplicitThrowSuspendCountVariable\);\s*SyncAnimationState\(creature, true\);")
            .Should().BeTrue(
                "the stale persisted counter must be cleared before the persistent remap is reapplied");
    }

    /// <summary>
    /// Locates the repository root used by the shared-pipeline source assertions.
    /// </summary>
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
