using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

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
        statusEffectBase.Should().Contain("HideEffectIcon(effect)",
            "native control effects provide mechanics while the tracked status supplies the one player-facing icon");
        statusEffectBase.Should().Contain(":Native:");
        statusEffectService.Should().Contain("statusEffect.RemoveNativeEffects(creature);");
    }

    [TestCase("BlindStatusEffect.cs", "EffectBlindness()")]
    [TestCase("DazedStatusEffect.cs", "EffectDazed()")]
    [TestCase("ImmobilizedStatusEffect.cs", "EffectCutsceneImmobilize()")]
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

    [TestCase("BlindStatusEffect.cs", "Blindness")]
    [TestCase("ConfusionStatusEffect.cs", "Confused")]
    [TestCase("DazedStatusEffect.cs", "Dazed")]
    [TestCase("ImmobilizedStatusEffect.cs", "Immobilized")]
    [TestCase("KnockdownStatusEffect.cs", "Knockdown")]
    [TestCase("StunnedStatusEffect.cs", "Stun")]
    [TestCase("TranquilizedStatusEffect.cs", "Sleep")]
    public void HardControlStatusEffects_StartSharedImmunityWhenRemoved(
        string fileName,
        string immunityType)
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "SWLOR.Game.Server",
            "Feature",
            "StatusEffectDefinition",
            fileName));

        source.Should().Contain("protected override void Remove(uint creature)");
        source.Should().Contain("if (IsBeingReplaced)");
        source.Should().Contain("Ability.ApplyPostControlImmunity(");
        source.Should().Contain("SecondsSinceNaturalExpiration");
        source.Should().Contain($"ImmunityType.{immunityType});");
        source.Should().NotContain(
            $"Ability.ApplyTemporaryImmunity(creature, duration, ImmunityType.{immunityType});",
            "hard-control immunity begins after the status ends, not while it is active");
    }

    [Test]
    public void StatusEffectReplacement_IsDistinguishedFromAnActualRemoval()
    {
        var replacement = new RemovalProbeStatusEffect();
        replacement.RemoveEffect(0u, true);
        replacement.WasReplacement.Should().BeTrue();

        var actualRemoval = new RemovalProbeStatusEffect();
        actualRemoval.RemoveEffect(0u);
        actualRemoval.WasReplacement.Should().BeFalse();
    }

    [TestCase("BlindStatusEffect.cs")]
    [TestCase("ConfusionStatusEffect.cs")]
    [TestCase("DazedStatusEffect.cs")]
    [TestCase("ImmobilizedStatusEffect.cs")]
    [TestCase("KnockdownStatusEffect.cs")]
    [TestCase("StunnedStatusEffect.cs")]
    [TestCase("TranquilizedStatusEffect.cs")]
    public void HardControlStatusEffects_RejectSameTypeRefreshBeforeReplacement(string fileName)
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "SWLOR.Game.Server",
            "Feature",
            "StatusEffectDefinition",
            fileName));
        var canApply = ExtractMethod(source, "public override string CanApply(uint creature)");

        var activeCheck = canApply.IndexOf(
            "StatusEffect.HasStatusEffect(creature, GetType())",
            StringComparison.Ordinal);
        var immunityCheck = canApply.IndexOf(
            "Ability.HasHardCrowdControlImmunity(",
            StringComparison.Ordinal);
        activeCheck.Should().BeGreaterThanOrEqualTo(0);
        activeCheck.Should().BeLessThan(
            immunityCheck,
            "an active hard-control instance must reject refresh before the non-stacking replacement path can remove it");
    }

    [Test]
    public void RepeatedHardControlRemovals_RestartTheSharedImmunityWindow()
    {
        var root = FindRepositoryRoot();
        var abilitySource = File.ReadAllText(Path.Combine(
            root,
            "SWLOR.Game.Server",
            "Service",
            "Ability.cs"));
        var methodStart = abilitySource.IndexOf(
            "private static void ApplyTemporaryImmunitySingle(",
            StringComparison.Ordinal);
        var methodEnd = abilitySource.IndexOf(
            "public static bool HasTemporaryImmunity(",
            methodStart,
            StringComparison.Ordinal);
        methodStart.Should().BeGreaterThanOrEqualTo(0);
        methodEnd.Should().BeGreaterThan(methodStart);

        var method = abilitySource[methodStart..methodEnd];
        method.Should().Contain("GetTemporaryImmunityDurationRemaining(target, effectTag)");
        method.Should().Contain("RemoveEffectByTag(target, effectTag);");
        method.Should().NotContain("if (HasTemporaryImmunity(target, immunity))",
            "each actual hard-control removal must restart the 20-second shared immunity");
        method.IndexOf("RemoveEffectByTag(target, effectTag);", StringComparison.Ordinal)
            .Should()
            .BeLessThan(method.IndexOf("ApplyEffectToObject(", StringComparison.Ordinal));
    }

    [Test]
    public void OfflineExpiration_TracksElapsedTimeAfterTheControlEnded()
    {
        var effect = new RemovalProbeStatusEffect();
        effect.ApplyEffect(0u, 0u, 2);

        effect.ReconcileElapsedTime(DateTime.UtcNow.AddSeconds(5));

        effect.IsFlaggedForRemoval.Should().BeTrue();
        effect.WasNaturallyExpired.Should().BeTrue();
        effect.SecondsSinceNaturalExpiration.Should().BeInRange(2.5f, 3.5f);
    }

    [Test]
    public void PostControlImmunity_AgesTheWindowDuringOfflineReconciliation()
    {
        var root = FindRepositoryRoot();
        var abilitySource = File.ReadAllText(Path.Combine(
            root,
            "SWLOR.Game.Server",
            "Service",
            "Ability.cs"));
        var method = ExtractMethod(abilitySource, "public static void ApplyPostControlImmunity(");

        method.Should().Contain(
            "TemporaryImmunityBaseDurationSeconds - Math.Max(0f, secondsSinceControlEnded)");
        method.Should().Contain("if (duration <= 0f)");
        method.Should().Contain("ApplyTemporaryImmunityForDuration(target, duration, immunity);");
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

    private static string ExtractMethod(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0);

        var braceStart = source.IndexOf('{', start);
        braceStart.Should().BeGreaterThan(start);

        var depth = 0;
        for (var index = braceStart; index < source.Length; index++)
        {
            if (source[index] == '{')
                depth++;
            else if (source[index] == '}' && --depth == 0)
                return source[start..(index + 1)];
        }

        throw new InvalidOperationException($"Could not extract method '{signature}'.");
    }

    [Test]
    public void HardCrowdControlEffects_DeclareTheCategoryTheSharedGateReads()
    {
        // While one hard CC runs, a different one must not land: Ability.HasHardCrowdControlImmunity
        // treats any active status carrying StatusEffectCategory.HardCrowdControl as immunity. An
        // effect that grants post-control immunity on Remove but forgets the category would be
        // stackable during its own duration - the regression this pins down.
        var root = FindRepositoryRoot();
        var definitionDirectory = Path.Combine(
            root, "SWLOR.Game.Server", "Feature", "StatusEffectDefinition");

        foreach (var file in Directory.EnumerateFiles(definitionDirectory, "*.cs"))
        {
            var source = File.ReadAllText(file);
            var grantsPostControlImmunity = source.Contains("ApplyPostControlImmunity");
            var declaresHardCrowdControl = source.Contains("StatusEffectCategory.HardCrowdControl");

            declaresHardCrowdControl.Should().Be(grantsPostControlImmunity,
                $"{Path.GetFileName(file)}: hard-CC effects grant post-control immunity AND " +
                "declare StatusEffectCategory.HardCrowdControl - one without the other either " +
                "stacks during its duration or blocks without ever releasing");
        }

        File.ReadAllText(Path.Combine(root, "SWLOR.Game.Server", "Service", "Ability.cs"))
            .Should().Contain("HasActiveHardCrowdControlStatus(target)",
                "the shared immunity gate must treat an active hard-CC status as immunity");
    }

    [Test]
    public void StatConfiguredIconAuditDetection_AgreesWithTheCompiledAttribute()
    {
        // The icon audit exempts a status effect from the one-class-one-icon model when
        // [StatConfiguredIcon] decorates the class declaration, detected textually in PowerShell.
        // The compiler is the ground truth for what is actually an attribute, so every definition
        // file's textual detection must agree with reflection: a marker sitting in a comment,
        // doc block, or string would make the audit skip a class the compiler never exempted,
        // and a declaration shape the pattern cannot see (partial, internal, abstract) would
        // make the audit enforce the full model on a class that is genuinely stat-configured.
        var root = FindRepositoryRoot();
        var definitionDirectory = Path.Combine(
            root, "SWLOR.Game.Server", "Feature", "StatusEffectDefinition");

        // Mirrors the detection in tools/UpdateGameplayIconStandards.ps1 - keep the two in sync.
        var declarationPattern = new System.Text.RegularExpressions.Regex(
            @"(?m)^\s*\[StatConfiguredIcon\]\s*(?:^\s*\[[^\]\r\n]+\]\s*)*(?:public|internal)\s+(?:(?:sealed|abstract|partial)\s+)*class\s");

        var attributedTypes = typeof(StatusEffectBase).Assembly
            .GetTypes()
            .Where(type => type.GetCustomAttributes(typeof(StatConfiguredIconAttribute), false).Length > 0)
            .Select(type => type.Name)
            .ToHashSet();

        foreach (var file in Directory.EnumerateFiles(definitionDirectory, "*.cs"))
        {
            var className = Path.GetFileNameWithoutExtension(file);
            var textualDetection = declarationPattern.IsMatch(File.ReadAllText(file));

            textualDetection.Should().Be(attributedTypes.Contains(className),
                $"{className}: the audit's textual [StatConfiguredIcon] detection must agree with " +
                "the compiled attribute - neither a marker in a comment/string nor an unsupported " +
                "declaration shape may make them diverge");
        }

        attributedTypes.Should().Contain(nameof(MeleeRepeatedTargetDamageStatusEffect),
            "the known stat-configured effect anchors this test's positive case");
    }

    private sealed class RemovalProbeStatusEffect : StatusEffectBase
    {
        public override string Name => "Removal Probe";
        public override EffectIconType Icon => EffectIconType.Invalid;
        public bool WasReplacement { get; private set; }

        protected override void Remove(uint creature)
        {
            WasReplacement = IsBeingReplaced;
        }
    }
}
