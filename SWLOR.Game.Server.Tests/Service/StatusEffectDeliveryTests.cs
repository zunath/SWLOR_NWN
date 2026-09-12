using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class StatusEffectDeliveryTests
{
    [Test]
    public void FiniteStatusEffects_CanBeExtendedWithoutBeingRemoved()
    {
        var effect = new LegacyDamageCallbackStatusEffect();
        effect.ApplyEffect(1, 2, 5);

        effect.ExtendDurationTicks(2);

        effect.DurationTicks.Should().Be(7);
        effect.IsFlaggedForRemoval.Should().BeFalse();
    }

    [Test]
    public void DurationResistanceFeedback_ReportsTheEffectiveDuration()
    {
        StatusEffect.BuildDurationResistanceMessage(ResistanceType.Mobility, "Immobilized", 10, 9, 3f)
            .Should().Be("Mobility Resistance reduced Immobilized duration from 30s to 27s.");
        StatusEffect.BuildDurationResistanceMessage(ResistanceType.Mind, "Confusion", 5, 6, 1f)
            .Should().Be("Mind Vulnerability increased Confusion duration from 5s to 6s.");
        StatusEffect.BuildDurationResistanceMessage(ResistanceType.Mind, "Dazed", 15, 17, 1f)
            .Should().Be("Mind Vulnerability increased Dazed duration from 15s to 17s.");
        StatusEffect.BuildDurationResistanceMessage(ResistanceType.Trauma, "Venom", 5, 5, 6f)
            .Should().BeEmpty();
    }

    [TestCase(30, 33, 30f, "")]
    [TestCase(36, 33, 30f, "")]
    [TestCase(30, 27, 30f, "Mind Resistance reduced Dazed duration from 30s to 27s.")]
    [TestCase(15, 17, 30f, "Mind Vulnerability increased Dazed duration from 15s to 17s.")]
    [TestCase(6, 7, 2.5f, "")]
    [TestCase(6, 4, 5.5f, "Mind Resistance reduced Dazed duration from 5s to 4s.")]
    public void ControlResistanceFeedback_UsesBothConstrainedDurations(
        int baselineTicks, int resistedTicks, float remainingSeconds, string expected)
    {
        int Constrain(int ticks) => StatusEffect.ClampConvertedControlDurationTicks(
            StatusEffect.ClampHardCrowdControlDurationTicks(StatusEffectCategory.HardCrowdControl, ticks, 1f),
            1f, remainingSeconds);

        StatusEffect.BuildDurationResistanceMessage(ResistanceType.Mind, "Dazed",
            Constrain(baselineTicks), Constrain(resistedTicks), 1f).Should().Be(expected,
            "feedback must report the remaining control time without crediting the control budget to resistance");
    }

    [Test]
    public void ControlResistanceFeedback_IsBuiltAfterBothRuntimeDurationConstraints()
    {
        var root = FindRepositoryRoot();
        var syntax = CSharpSyntaxTree.ParseText(File.ReadAllText(
            Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "StatusEffect.cs"))).GetRoot();
        var method = syntax.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(node => node.Identifier.ValueText == "ApplyStatusEffectInternal");
        var feedback = method.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Single(node => node.Expression.ToString() == "BuildDurationResistanceMessage");
        foreach (var constraint in new[] { "ClampHardCrowdControlDurationTicks", "ClampConvertedControlDurationTicks" })
        {
            var assignments = method.DescendantNodes().OfType<AssignmentExpressionSyntax>()
                .Where(node => node.Right is InvocationExpressionSyntax call && call.Expression.ToString() == constraint)
                .ToArray();
            assignments.Select(node => node.Left.ToString()).Should().BeEquivalentTo(
                "durationTicks", "durationTicksWithoutResistance");
            assignments.Should().OnlyContain(node => node.Span.End < feedback.Span.Start,
                "the runtime must clamp both durations before formatting feedback");
        }
    }

    [Test]
    public void ForceAffinity_DoesNotAlterStatusDuration()
    {
        var root = FindRepositoryRoot();
        var statusEffectSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "StatusEffect.cs"));
        var abilitySource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "Ability.cs"));

        statusEffectSource.Should().NotContain("ApplyActiveForceAffinityDurationAdjustment");
        abilitySource.Should().NotContain("ApplyActiveForceAffinityDurationAdjustment");
    }

    [Test]
    public void SubdualSelfApplication_IsPassedToBothControlBudgetClamps()
    {
        var root = FindRepositoryRoot();
        var statusSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "StatusEffect.cs"));
        var calls = CSharpSyntaxTree.ParseText(statusSource).GetRoot().DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(call => call.Expression.ToString() == "ClampHardCrowdControlDurationTicks").ToArray();
        calls.Should().HaveCount(2);
        calls.Should().OnlyContain(call => call.ArgumentList.Arguments.Any(arg =>
            arg.NameColon != null && arg.NameColon.Name.Identifier.ValueText == "isSelfApplied" &&
            arg.Expression.ToString() == "source == creature"));
        File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "Death.cs"))
            .Should().Contain("StatusEffect.ApplyStatusEffect(player, player, typeof(KnockdownStatusEffect), 60f);");
    }

    [Test]
    public void LegacyDamageCallbacks_OnlyReceiveDirectDamage()
    {
        var effect = new LegacyDamageCallbackStatusEffect();

        effect.OnDamageDealtEffect(1, 2, 10, CombatDamageType.Physical, CombatDamageDeliveryType.Direct);
        effect.OnDamageDealtEffect(1, 2, 10, CombatDamageType.Physical, CombatDamageDeliveryType.Triggered);
        effect.OnDamageDealtEffect(1, 2, 10, CombatDamageType.Physical, CombatDamageDeliveryType.DamageOverTime);

        effect.OnDamageTakenEffect(2, 1, 10, CombatDamageType.Physical, CombatDamageDeliveryType.Direct);
        effect.OnDamageTakenEffect(2, 1, 10, CombatDamageType.Physical, CombatDamageDeliveryType.Triggered);
        effect.OnDamageTakenEffect(2, 1, 10, CombatDamageType.Physical, CombatDamageDeliveryType.DamageOverTime);

        effect.LegacyDamageDealtCalls.Should().Be(1);
        effect.LegacyDamageTakenCalls.Should().Be(1);
    }

    private sealed class LegacyDamageCallbackStatusEffect : StatusEffectBase
    {
        public override string Name => "Legacy Damage Callback";
        public override EffectIconType Icon => EffectIconType.Invalid;
        public int LegacyDamageDealtCalls { get; private set; }
        public int LegacyDamageTakenCalls { get; private set; }

        protected override void OnDamageDealt(uint attacker, uint defender, int damage, CombatDamageType damageType)
        {
            LegacyDamageDealtCalls++;
        }

        protected override void OnDamageTaken(uint defender, uint attacker, int damage, CombatDamageType damageType)
        {
            LegacyDamageTakenCalls++;
        }
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("repository root should be discoverable from the test directory");
        return directory!;
    }
}
