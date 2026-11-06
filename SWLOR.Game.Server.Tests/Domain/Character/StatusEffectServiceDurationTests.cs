using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>Restarting, extending and refreshing an active status, the duration rules and the duration the engine reports.</summary>
[TestFixture]
public class StatusEffectServiceDurationTests : StatusEffectServiceTestBase
{
    [Test]
    public void Restart_RefusesAMissingCreatureTypeOrDuration()
    {
        Service.RestartStatusEffectDuration(Target, typeof(ProbeEffect), Caster, 5f).Should().BeFalse();

        Applied<ProbeEffect>(Target, Caster, seconds: 5f);
        Service.RestartStatusEffectDuration(Target, null!, Caster, 5f).Should().BeFalse();
        Service.RestartStatusEffectDuration(Target, typeof(ProbeEffect), Caster, 0f).Should().BeFalse();
    }

    [Test]
    public void Restart_NeedsExactlyOneInstanceFromThatSource()
    {
        Service.GetCreatureStatusEffects(Target);
        Applied<ProbeEffect>(Target, Caster, probe => probe.StackValue = StatusEffectStackType.UnlimitedStacking, 5f);
        Service.RestartStatusEffectDuration(Target, typeof(ProbeEffect), Bystander, 5f).Should().BeFalse();

        Applied<ProbeEffect>(Target, Caster, probe => probe.StackValue = StatusEffectStackType.UnlimitedStacking, 5f);
        Service.RestartStatusEffectDuration(Target, typeof(ProbeEffect), Caster, 5f).Should().BeFalse();
    }

    [Test]
    public void Restart_RefusesFlaggedPermanentAndHardControlEffects()
    {
        var flagged = Applied<ProbeEffect>(Target, Caster, seconds: 5f);
        flagged.Flag();
        Service.RestartStatusEffectDuration(Target, typeof(ProbeEffect), Caster, 5f).Should().BeFalse();

        Applied<OtherProbeEffect>(Target, Caster);
        Service.RestartStatusEffectDuration(Target, typeof(OtherProbeEffect), Caster, 5f).Should().BeFalse();

        Applied<ThirdProbeEffect>(Target, Caster, probe => probe.CategoriesValue = StatusEffectCategory.HardCrowdControl, 5f);
        Service.RestartStatusEffectDuration(Target, typeof(ThirdProbeEffect), Caster, 5f).Should().BeFalse();
    }

    [Test]
    public void Restart_RunsTheDurationRulesAgainAndKeepsTheEffect()
    {
        Bench.Hostile.Add((Target, Caster));
        Bench.Players.Add(Caster);
        Service.Stats[(Caster, StatType.OutgoingDebuffDurationPercentAdjustment)] = 50;
        Bench.ResistedTicks = ticks => ticks - 1;
        Bench.Recording = true;
        var effect = Applied<ProbeEffect>(Target, Caster, probe => probe.CategoriesValue = StatusEffectCategory.Debuff, 5f);
        effect.AssignResistanceType(ResistanceType.Mind);
        Now = Start.AddSeconds(3);
        Bench.Engine.ClearCalls();

        Service.RestartStatusEffectDuration(Target, typeof(ProbeEffect), Caster, 10f).Should().BeTrue();

        effect.DurationTicks.Should().Be(14);
        effect.GetRemainingDurationSeconds(Now).Should().Be(14f);
        Log.Should().Contain($"CalculateResistedTicks:{Target}:{ResistanceType.Mind}:15");
        Bench.Engine.CountOf("ApplyEffectToObject").Should().Be(1);
        Bench.Engine.CallsTo("RemoveEffectByTag").Should().ContainSingle(call => ((string[])call.Arguments[1]!).Contains(effect.Id));
        Bench.Messages.Single().Message.Should().Be("Mind Resistance reduced Probe duration from 15s to 14s.");
    }

    [Test]
    public void Restart_OfAnImmuneOrFullyResistedTargetDoesNothing()
    {
        Bench.Hostile.Add((Target, Caster));
        var effect = Applied<ProbeEffect>(Target, Caster, seconds: 5f);
        effect.AssignResistanceType(ResistanceType.Mind);

        Bench.Immune.Add((Target, ResistanceType.Mind));
        Service.RestartStatusEffectDuration(Target, typeof(ProbeEffect), Caster, 10f).Should().BeFalse();

        Bench.Immune.Clear();
        Bench.ResistedTicks = _ => 0;
        Service.RestartStatusEffectDuration(Target, typeof(ProbeEffect), Caster, 10f).Should().BeFalse();
        effect.DurationTicks.Should().Be(5);
    }

    [TestCase(Invalid)]
    public void Restart_WithAnInvalidSourceSkipsResistance(uint source)
    {
        var effect = Applied<ProbeEffect>(Target, source, seconds: 5f);
        effect.AssignResistanceType(ResistanceType.Mind);
        Bench.Hostile.Add((Target, source));

        Service.RestartStatusEffectDuration(Target, typeof(ProbeEffect), source, 10f).Should().BeTrue();

        Log.Should().NotContain(line => line.StartsWith("HasImmunity"));
    }

    [Test]
    public void Restart_OnAFriendlyTargetSkipsResistance()
    {
        var effect = Applied<ProbeEffect>(Target, Caster, seconds: 5f);
        effect.AssignResistanceType(ResistanceType.Mind);

        Service.RestartStatusEffectDuration(Target, typeof(ProbeEffect), Caster, 10f).Should().BeTrue();

        Log.Should().NotContain(line => line.StartsWith("HasImmunity"));
    }

    [Test]
    public void Extend_RefusesAMissingCreatureTypeOrDuration()
    {
        Service.ExtendStatusEffectDuration(Target, typeof(ProbeEffect), Caster, 5f).Should().BeFalse();

        Applied<ProbeEffect>(Target, Caster, seconds: 5f);
        Service.ExtendStatusEffectDuration(Target, null!, Caster, 5f).Should().BeFalse();
        Service.ExtendStatusEffectDuration(Target, typeof(ProbeEffect), Caster, -1f).Should().BeFalse();
    }

    [Test]
    public void Extend_AddsToTheRemainingTimeOfTheMatchingEffects()
    {
        var mine = Applied<ProbeEffect>(Target, Caster, probe => probe.StackValue = StatusEffectStackType.UnlimitedStacking, 5f);
        var theirs = Applied<ProbeEffect>(Target, Bystander, probe => probe.StackValue = StatusEffectStackType.UnlimitedStacking, 5f);
        Applied<OtherProbeEffect>(Target, Caster, seconds: 5f);
        var hard = Applied<ThirdProbeEffect>(Target, Caster, probe => probe.CategoriesValue = StatusEffectCategory.HardCrowdControl, 5f);
        Now = Start.AddSeconds(2);
        Bench.Recording = true;

        Service.ExtendStatusEffectDuration(Target, typeof(ProbeEffect), Caster, 4f).Should().BeTrue();

        mine.GetRemainingDurationSeconds(Now).Should().Be(7f);
        theirs.GetRemainingDurationSeconds(Now).Should().Be(3f);
        Log.Should().ContainSingle(line => line.StartsWith("RecordStatusApplied"));

        Service.ExtendStatusEffectDuration(Target, typeof(ThirdProbeEffect), Caster, 4f).Should().BeFalse("control cannot be extended");
        hard.GetRemainingDurationSeconds(Now).Should().Be(3f);
    }

    [Test]
    public void Extend_WithNoSourceFilterTakesEveryInstance()
    {
        var mine = Applied<ProbeEffect>(Target, Caster, probe => probe.StackValue = StatusEffectStackType.UnlimitedStacking, 5f);
        var theirs = Applied<ProbeEffect>(Target, Bystander, probe => probe.StackValue = StatusEffectStackType.UnlimitedStacking, 5f);

        Service.ExtendStatusEffectDuration(Target, typeof(ProbeEffect), Invalid, 4f).Should().BeTrue();

        mine.GetRemainingDurationSeconds(Now).Should().Be(9f);
        theirs.GetRemainingDurationSeconds(Now).Should().Be(9f);
    }

    [Test]
    public void Refresh_RefusesAMissingCreatureTypeOrDuration()
    {
        Service.RefreshStatusEffectDuration(Target, typeof(ProbeEffect), Caster, 5f).Should().BeFalse();

        Applied<ProbeEffect>(Target, Caster, seconds: 5f);
        Service.RefreshStatusEffectDuration(Target, null!, Caster, 5f).Should().BeFalse();
        Service.RefreshStatusEffectDuration(Target, typeof(ProbeEffect), Caster, 0f).Should().BeFalse();
    }

    [Test]
    public void Refresh_TakesTheLongerOfWhatIsLeftAndTheNewDuration()
    {
        var effect = Applied<ProbeEffect>(Target, Caster, seconds: 10f);
        Bench.Recording = true;

        Service.RefreshStatusEffectDuration(Target, typeof(ProbeEffect), Caster, 5f).Should().BeTrue();
        effect.GetRemainingDurationSeconds(Now).Should().Be(10f);

        Service.RefreshStatusEffectDuration(Target, typeof(ProbeEffect), Caster, 20f).Should().BeTrue();
        effect.GetRemainingDurationSeconds(Now).Should().Be(20f);
        Log.Count(line => line.StartsWith("RecordStatusApplied")).Should().Be(2);
    }

    [Test]
    public void Refresh_SkipsOtherTypesSourcesAndHardControl()
    {
        var mine = Applied<ProbeEffect>(Target, Caster, seconds: 10f);
        Applied<OtherProbeEffect>(Target, Caster, seconds: 10f);
        Applied<ThirdProbeEffect>(Target, Caster, probe => probe.CategoriesValue = StatusEffectCategory.HardCrowdControl, 10f);

        Service.RefreshStatusEffectDuration(Target, typeof(ProbeEffect), Bystander, 20f).Should().BeFalse();
        mine.GetRemainingDurationSeconds(Now).Should().Be(10f);

        Service.RefreshStatusEffectDuration(Target, typeof(ThirdProbeEffect), Caster, 20f).Should().BeFalse();

        Service.RefreshStatusEffectDuration(Target, typeof(ProbeEffect), Invalid, 20f).Should().BeTrue();
        mine.GetRemainingDurationSeconds(Now).Should().Be(20f);
    }

    [Test]
    public void Refresh_OfAnImmuneTargetIsResistedAndLeavesTheEffect()
    {
        Bench.Hostile.Add((Target, Caster));
        var effect = Applied<ProbeEffect>(Target, Caster, seconds: 5f);
        Bench.Immune.Add((Target, ResistanceType.Mind));

        Service.RefreshStatusEffectDuration(Target, typeof(ProbeEffect), Caster, 20f, ResistanceType.Mind).Should().BeFalse();

        Bench.Messages.Should().Equal((Caster, "Your ability was resisted."));
        effect.GetRemainingDurationSeconds(Now).Should().Be(5f);
    }

    [Test]
    public void Refresh_ThatResistanceBringsToNoTicksIsResisted()
    {
        Bench.Hostile.Add((Target, Caster));
        Applied<ProbeEffect>(Target, Caster, seconds: 5f);
        Bench.ResistedTicks = _ => 0;

        Service.RefreshStatusEffectDuration(Target, typeof(ProbeEffect), Caster, 20f, ResistanceType.Mind).Should().BeFalse();

        Bench.Messages.Should().Equal((Caster, "Your ability was resisted."));
    }

    [Test]
    public void Refresh_UsesTheOutgoingBonusBeforeResistanceAndTheDamageTypeForResistance()
    {
        Bench.Hostile.Add((Target, Caster));
        Service.Stats[(Caster, StatType.OutgoingDebuffDurationPercentAdjustment)] = 100;
        Bench.ResistedTicks = ticks => ticks + 2;
        var effect = Applied<ProbeEffect>(Target, Caster, probe => probe.CategoriesValue = StatusEffectCategory.Debuff, 5f);

        Service.RefreshStatusEffectDuration(Target, typeof(ProbeEffect), Caster, 10f, ResistanceType.Invalid, CombatDamageType.Fire).Should().BeTrue();

        Log.Should().Contain($"CalculateResistedTicks:{Target}:{ResistanceType.Fire}:20");
        effect.GetRemainingDurationSeconds(Now).Should().Be(22f);
    }

    [Test]
    public void Refresh_OnAFriendlyTargetOrFromAnInvalidSourceSkipsResistance()
    {
        Applied<ProbeEffect>(Target, Caster, seconds: 5f);

        Service.RefreshStatusEffectDuration(Target, typeof(ProbeEffect), Caster, 10f, ResistanceType.Mind).Should().BeTrue();
        Bench.Invalid.Add(Caster);
        Bench.Hostile.Add((Target, Caster));
        Service.RefreshStatusEffectDuration(Target, typeof(ProbeEffect), Caster, 10f, ResistanceType.Mind).Should().BeTrue();

        Log.Should().NotContain(line => line.StartsWith("HasImmunity"));
    }

    [TestCase(ResistanceType.Invalid, 10, 5)]
    [TestCase(ResistanceType.Mind, 0, 5)]
    [TestCase(ResistanceType.Mind, 10, 0)]
    [TestCase(ResistanceType.Mind, 10, 10)]
    public void TheResistanceMessageIsEmptyWhenThereIsNothingToSay(ResistanceType type, int original, int adjusted)
    {
        Service.BuildDurationResistanceMessage(type, "Stun", original, adjusted, 1f).Should().BeEmpty();
    }

    [Test]
    public void TheResistanceMessageNamesTheDirectionAndFormatsTheSeconds()
    {
        Service.BuildDurationResistanceMessage(ResistanceType.Mind, "Stun", 10, 4, 1f)
            .Should().Be("Mind Resistance reduced Stun duration from 10s to 4s.");
        Service.BuildDurationResistanceMessage(ResistanceType.Mind, "Stun", 4, 10, 6f)
            .Should().Be("Mind Vulnerability increased Stun duration from 24s to 60s.");
        Service.BuildDurationResistanceMessage(ResistanceType.Mind, " ", 4, 10, 1.5f)
            .Should().Be("Mind Vulnerability increased the effect duration from 6s to 15s.");
        Service.BuildDurationResistanceMessage(ResistanceType.Mind, "Stun", 1, 3, 1.5f)
            .Should().Be("Mind Vulnerability increased Stun duration from 1.5s to 4.5s.");
    }

    [Test]
    public void TheOutgoingAdjustmentAddsEveryCategoryTheEffectHas()
    {
        int Stat(StatType stat) => stat switch
        {
            StatType.OutgoingDebuffDurationPercentAdjustment => 1,
            StatType.OutgoingControlDurationPercentAdjustment => 10,
            StatType.OutgoingForceDisruptionDurationPercentAdjustment => 100,
            StatType.OutgoingAbilityDisruptionDurationPercentAdjustment => 1000,
            _ => 0
        };

        Service.GetOutgoingDurationPercentAdjustment(StatusEffectCategory.None, Stat).Should().Be(0);
        Service.GetOutgoingDurationPercentAdjustment(StatusEffectCategory.Debuff, Stat).Should().Be(1);
        Service.GetOutgoingDurationPercentAdjustment(StatusEffectCategory.Control, Stat).Should().Be(10);
        Service.GetOutgoingDurationPercentAdjustment(StatusEffectCategory.ForceDisruption, Stat).Should().Be(100);
        Service.GetOutgoingDurationPercentAdjustment(StatusEffectCategory.AbilityDisruption, Stat).Should().Be(1000);
        Service.GetOutgoingDurationPercentAdjustment(StatusEffectCategory.Debuff | StatusEffectCategory.AbilityDisruption, Stat).Should().Be(1001);
        StatusEffectRules.GetOutgoingDurationPercentAdjustment(StatusEffectCategory.Control, Stat).Should().Be(10);
    }

    [TestCase(10f, 50, 15f)]
    [TestCase(10f, -100, 0.1f)]
    [TestCase(10f, 0, 10f)]
    public void TheAdjustedDurationNeverDropsBelowATenthOfASecond(float seconds, int percent, float expected)
    {
        Service.CalculateAdjustedStatusDurationSeconds(seconds, percent).Should().Be(expected);
        StatusEffectRules.CalculateAdjustedStatusDurationSeconds(seconds, percent).Should().Be(expected);
    }

    [Test]
    public void TheControlBudgetAppliesOnlyToHardControlFromAnother()
    {
        const StatusEffectCategory Hard = StatusEffectCategory.HardCrowdControl;

        Service.ClampHardCrowdControlDurationTicks(StatusEffectCategory.Debuff, 100, 1f).Should().Be(100);
        Service.ClampHardCrowdControlDurationTicks(Hard, 0, 1f).Should().Be(0);
        Service.ClampHardCrowdControlDurationTicks(Hard, -1, 1f).Should().Be(-1);
        Service.ClampHardCrowdControlDurationTicks(Hard, 100, 1f, isSelfApplied: true).Should().Be(100);
        Service.ClampHardCrowdControlDurationTicks(Hard, 100, 1f).Should().Be(30);
        Service.ClampHardCrowdControlDurationTicks(Hard, 100, 6f).Should().Be(5);
        Service.ClampHardCrowdControlDurationTicks(Hard, 100, 0.5f).Should().Be(30);
        Service.ClampHardCrowdControlDurationTicks(Hard, 100, 1f, isPlayerAgainstPlayer: true).Should().Be(15);
        Service.ClampHardCrowdControlDurationTicks(Hard, 10, 1f).Should().Be(10);
    }

    [TestCase(10, 1f, -1f, 10)]
    [TestCase(10, 1f, 4.5f, 4)]
    [TestCase(10, 2f, 4f, 2)]
    [TestCase(3, 1f, 10f, 3)]
    [TestCase(3, 0.5f, 10f, 3)]
    public void AConversionKeepsTheOriginalExpiry(int requested, float frequency, float remaining, int expected)
    {
        Service.ClampConvertedControlDurationTicks(requested, frequency, remaining).Should().Be(expected);
    }

    [Test]
    public void TheEffectDurationIsZeroWhenThereIsNoTimedEffect()
    {
        Service.GetEffectDuration(Target, typeof(ProbeEffect)).Should().Be(0);

        Applied<ProbeEffect>(Target, Caster);
        Service.GetEffectDuration(Target, typeof(ProbeEffect)).Should().Be(0, "it is permanent");
        Service.GetEffectDuration(Target, typeof(OtherProbeEffect)).Should().Be(0);
    }

    [Test]
    public void TheEffectDurationComesFromTheLongestNativeEffectWithItsTag()
    {
        var effect = Applied<ProbeEffect>(Target, Caster, seconds: 5f);
        var engineEffects = new[]
        {
            (Handle: EngineStructures.Create<Effect>(1), Tag: "other", Remaining: 90),
            (Handle: EngineStructures.Create<Effect>(2), Tag: effect.Id, Remaining: 12),
            (Handle: EngineStructures.Create<Effect>(3), Tag: effect.Id, Remaining: 34),
        };
        var position = -1;
        Effect Next() => ++position < engineEffects.Length ? engineEffects[position].Handle : EngineStructures.Create<Effect>(0);
        Bench.Engine.SetReturnHandler("GetFirstEffect", _ =>
        {
            position = -1;
            return Next();
        });
        Bench.Engine.SetReturnHandler("GetNextEffect", _ => Next());
        Bench.Engine.SetReturnHandler("GetIsEffectValid", args => ((Effect)args[0]!).Handle != 0);
        Bench.Engine.SetReturnHandler("GetEffectTag", args => engineEffects.Single(entry => entry.Handle.Handle == ((Effect)args[0]!).Handle).Tag);
        Bench.Engine.SetReturnHandler("GetEffectDurationRemaining", args => engineEffects.Single(entry => entry.Handle.Handle == ((Effect)args[0]!).Handle).Remaining);

        Service.GetEffectDuration(Target, typeof(OtherProbeEffect), typeof(ProbeEffect)).Should().Be(34);
    }

    [Test]
    public void TheEffectDurationFallsBackToItsOwnTicksWithoutANativeEffect()
    {
        Applied<ProbeEffect>(Target, Caster, probe => probe.FrequencyValue = 6f, 12f);

        Service.GetEffectDuration(Target, typeof(ProbeEffect)).Should().Be(12);
    }

    [Test]
    public void ATimedEffectWithNoLogicalDurationUsesItsTicks()
    {
        Service.ApplyStatusEffect(Caster, Target, Probe<ProbeEffect>(probe => probe.ActivationValue = StatusEffectActivationType.Passive), 5f);
        Bench.Engine.CallsTo("ApplyEffectToObject").Single().Arguments[3].Should().Be(5f);
    }
}
