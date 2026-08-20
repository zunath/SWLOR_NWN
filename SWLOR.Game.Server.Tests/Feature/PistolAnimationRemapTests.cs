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
        PistolAnimationRemap.PistolShotSound.Should().Be("cb_sh_blstrfire1");
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
    /// Verifies only an active replacement animation needs a scripted pistol shot sound.
    /// </summary>
    [TestCase(true, BaseItem.Pistol, null, true)]
    [TestCase(true, BaseItem.LegacyPistol, null, true)]
    [TestCase(true, BaseItem.Sling, null, true)]
    [TestCase(false, BaseItem.Pistol, null, false)]
    [TestCase(true, BaseItem.Pistol, BaseItem.SmallShield, false)]
    [TestCase(true, BaseItem.Rifle, null, false)]
    public void ReplacementShotSound_PlaysOnlyForAnActiveEligiblePistolRemap(
        bool isRemapActive,
        BaseItem? rightHandBaseItem,
        BaseItem? leftHandBaseItem,
        bool expected)
    {
        var result = PistolAnimationRemap.ShouldPlayRemappedPistolShotSound(
            isRemapActive,
            rightHandBaseItem,
            leftHandBaseItem);

        result.Should().Be(expected);
    }

    /// <summary>
    /// Verifies weapon projectile launches use the handler that restores the remapped pistol shot sound.
    /// </summary>
    [Test]
    public void WeaponProjectiles_RestoreTheRemappedPistolShotSound()
    {
        var method = typeof(PistolAnimationRemap).GetMethod(nameof(PistolAnimationRemap.OnWeaponProjectileCreated));
        var scripts = method!
            .GetCustomAttributes(typeof(NWNEventHandler), false)
            .Cast<NWNEventHandler>()
            .Select(attribute => attribute.Script);

        scripts.Should().Contain(ScriptName.OnBroadcastSafeProjectileBefore);

        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "PistolAnimationRemap.cs"));
        source.Should().Contain("AssignCommand(attacker, () => PlaySound(PistolShotSound));");

        var registrationSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "EventRegistration.cs")).ReplaceLineEndings("\n");
        registrationSource.Should().Contain(
            "EventsPlugin.SubscribeEvent(\n                \"NWNX_ON_BROADCAST_SAFE_PROJECTILE_BEFORE\",\n                ScriptName.OnBroadcastSafeProjectileBefore);");
        registrationSource.Should().Contain(
            "EventsPlugin.AddIDToWhitelist(\n                    \"NWNX_ON_BROADCAST_SAFE_PROJECTILE_TYPE\",\n                    projectileType);");
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
    /// Verifies a lifecycle reset invalidates pending callbacks without rejecting a subsequent throw.
    /// </summary>
    [Test]
    public void LifecycleReset_InvalidatesPendingCallbackButAllowsSubsequentThrowCallback()
    {
        const int pendingCallbackGeneration = 7;
        const int currentGenerationAfterReset = pendingCallbackGeneration + 1;
        const int subsequentThrowCallbackGeneration = currentGenerationAfterReset;

        PistolAnimationRemap.ShouldApplyDelayedAnimationCallback(
                pendingCallbackGeneration,
                currentGenerationAfterReset)
            .Should().BeFalse();
        PistolAnimationRemap.ShouldApplyDelayedAnimationCallback(
                subsequentThrowCallbackGeneration,
                currentGenerationAfterReset)
            .Should().BeTrue();
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
                @"SetLocalInt\(\s*creature,\s*AnimationCallbackGenerationVariable,\s*unchecked\(GetLocalInt\(creature, AnimationCallbackGenerationVariable\) \+ 1\)\);\s*DeleteLocalInt\(creature, ExplicitThrowSuspendCountVariable\);\s*SyncAnimationState\(creature, true\);")
            .Should().BeTrue(
                "pending callbacks must be invalidated and the stale counter cleared before the remap is reapplied");
    }

    /// <summary>
    /// Verifies both delayed restoration paths reject callbacks from a prior lifecycle generation.
    /// </summary>
    [Test]
    public void DelayedRestorationCallbacks_ValidateCapturedLifecycleGeneration()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "PistolAnimationRemap.cs"));
        var temporaryReplacementMethod = ExtractMethod(
            source,
            "public static void PlayAnimationWithTemporaryReplacementPreservingExplicitThrow(");
        var remapRestorationMethod = ExtractMethod(
            source,
            "private static void ScheduleRemapAfterExplicitThrow(");
        const string generationCapture =
            "var callbackGeneration = GetLocalInt(creature, AnimationCallbackGenerationVariable);";

        temporaryReplacementMethod.Should().Contain(generationCapture);
        temporaryReplacementMethod.IndexOf(generationCapture, StringComparison.Ordinal)
            .Should().BeLessThan(temporaryReplacementMethod.IndexOf(
                "DelayCommand(",
                StringComparison.Ordinal));
        temporaryReplacementMethod.Should().Contain(
            "if (!ShouldApplyDelayedAnimationCallback(creature, callbackGeneration))");
        temporaryReplacementMethod.IndexOf(
                "if (!ShouldApplyDelayedAnimationCallback(creature, callbackGeneration))",
                StringComparison.Ordinal)
            .Should().BeLessThan(temporaryReplacementMethod.IndexOf(
                "ReplaceObjectAnimation(creature, sourceAnimationName);",
                StringComparison.Ordinal));
        remapRestorationMethod.Should().Contain(generationCapture);
        remapRestorationMethod.IndexOf(generationCapture, StringComparison.Ordinal)
            .Should().BeLessThan(remapRestorationMethod.IndexOf(
                "DelayCommand(",
                StringComparison.Ordinal));
        remapRestorationMethod.Should().Contain(
            "if (!ShouldApplyDelayedAnimationCallback(creature, callbackGeneration))");
        remapRestorationMethod.IndexOf(
                "if (!ShouldApplyDelayedAnimationCallback(creature, callbackGeneration))",
                StringComparison.Ordinal)
            .Should().BeLessThan(remapRestorationMethod.IndexOf(
                "var suspendCount = GetLocalInt(creature, ExplicitThrowSuspendCountVariable);",
                StringComparison.Ordinal));
    }

    /// <summary>
    /// Extracts a method body for source-level animation callback assertions.
    /// </summary>
    private static string ExtractMethod(string source, string signature)
    {
        var methodStart = source.IndexOf(signature, StringComparison.Ordinal);
        methodStart.Should().BeGreaterThanOrEqualTo(0, $"{signature} should exist");
        var openingBrace = source.IndexOf('{', methodStart);
        openingBrace.Should().BeGreaterThan(methodStart);

        var depth = 0;
        for (var index = openingBrace; index < source.Length; index++)
        {
            if (source[index] == '{')
                depth++;
            else if (source[index] == '}')
                depth--;

            if (depth == 0)
                return source.Substring(methodStart, index - methodStart + 1);
        }

        throw new InvalidOperationException($"Could not find the end of {signature}.");
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
