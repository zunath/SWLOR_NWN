using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>Applying a status: the order of everything an application does, and every way it can be refused.</summary>
[TestFixture]
public class StatusEffectServiceApplyTests : StatusEffectServiceTestBase
{
    private static readonly AbilityDetail ActiveAbility = new() { Name = "Active" };

    private ProbeEffect HardControl(Action<ProbeEffect>? setup = null) => Probe<ProbeEffect>(probe =>
    {
        probe.CategoriesValue = StatusEffectCategory.HardCrowdControl;
        setup?.Invoke(probe);
    });

    [Test]
    public void AHostileDebuff_RunsEveryStepInTheOriginalOrder()
    {
        Bench.Hostile.Add((Target, Caster));
        Service.ActiveImpact = new AbilityImpactSummary { Ability = ActiveAbility };
        Bench.ResistedTicks = ticks => ticks - 2;
        var effect = Probe<ProbeEffect>(probe =>
        {
            probe.CategoriesValue = StatusEffectCategory.Debuff;
            probe.ResistanceValue = ResistanceType.Mind;
            probe.StatGroup.Stats[StatType.Attack] = -3;
        });

        Service.ApplyStatusEffect(Caster, Target, effect, 10f).Should().BeTrue();

        Log.Should().Equal(
            $"HasImmunity:{Target}:{ResistanceType.Mind}",
            $"CalculateResistedTicks:{Target}:{ResistanceType.Mind}:10",
            "CanApply:Probe",
            $"Apply:Probe:{Target}:8",
            $"ApplyCreatureMovementRate:{Target}",
            $"StaminaDrain:{Caster}:{Target}:{StatusEffectCategory.Debuff}",
            $"HostileBuff:{Caster}:Active:0:True",
            $"Refresh:{Target}:StatusEffectReceivedRefreshEvent",
            $"Nearby:{Target}:name-{Target} receives the effect of Probe from name-{Caster}",
            $"CapstoneProof:{Caster}:{Target}:ProbeEffect:Active");
        effect.OriginatingAbility.Should().BeSameAs(ActiveAbility);
        effect.AppliedResistanceType.Should().Be(ResistanceType.Mind);
        Service.GetStatusEffect(Target, typeof(ProbeEffect)).Should().BeSameAs(effect);
    }

    [Test]
    public void TheNativeEffectCarriesTheStatusIdAndIsTemporaryForATimedStatus()
    {
        var effect = Probe<ProbeEffect>(probe => probe.IconValue = EffectIconType.Stunned);

        Service.ApplyStatusEffect(Caster, Target, effect, 5f);

        var apply = Bench.Engine.CallsTo("ApplyEffectToObject").Single();
        apply.Arguments[0].Should().Be(DurationType.Temporary);
        apply.Arguments[2].Should().Be(Target);
        apply.Arguments[3].Should().Be(6f, "a ticking status keeps one tick of grace past its 5 seconds");
        Bench.Engine.CallsTo("EffectRunScript").Single().Arguments[4].Should().Be(effect.Id);
        Bench.Engine.CallsTo("EffectIcon").Single().Arguments[0].Should().Be(EffectIconType.Stunned);
        Bench.Engine.CallsTo("TagEffect").Should().OnlyContain(call => (string)call.Arguments[1]! == effect.Id);
        Bench.Engine.CountOf("SupernaturalEffect").Should().Be(1);
    }

    [Test]
    public void APassiveStatusHasNoIntervalScriptAndKeepsItsExactDuration()
    {
        var effect = Probe<ProbeEffect>(probe => probe.ActivationValue = StatusEffectActivationType.Passive);

        Service.ApplyStatusEffect(Caster, Target, effect, 5f);

        Bench.Engine.CallsTo("EffectRunScript").Single().Arguments[2].Should().Be(string.Empty);
        Bench.Engine.CallsTo("ApplyEffectToObject").Single().Arguments[3].Should().Be(5f);
    }

    [Test]
    public void NoDuration_MeansAPermanentNativeEffectWithoutResistance()
    {
        var effect = Probe<ProbeEffect>(probe => probe.ResistanceValue = ResistanceType.Mind);
        Bench.Hostile.Add((Target, Caster));

        Service.ApplyStatusEffect(Caster, Target, effect, 0f).Should().BeTrue();

        Bench.Engine.CallsTo("ApplyEffectToObject").Single().Arguments[0].Should().Be(DurationType.Permanent);
        Log.Should().NotContain(line => line.StartsWith("HasImmunity"));
        effect.DurationTicks.Should().Be(-1);
    }

    [Test]
    public void TheGenericAndPermanentOverloadsMakeANewEffect()
    {
        Service.ApplyPermanentStatusEffect<ProbeEffect>(Caster, Target);
        Service.ApplyPermanentStatusEffect(typeof(OtherProbeEffect), Caster, Target);
        Service.ApplyStatusEffect<ThirdProbeEffect>(Caster, Target, 5f).Should().BeTrue();
        Service.ApplyStatusEffect<LimitedDelayProbeEffect>(Caster, Target, 5f, CombatDamageType.Physical).Should().BeTrue();

        Service.GetStatusEffect(Target, typeof(ProbeEffect))!.DurationTicks.Should().Be(-1);
        Service.GetStatusEffect(Target, typeof(OtherProbeEffect))!.DurationTicks.Should().Be(-1);
        Service.GetStatusEffect(Target, typeof(ThirdProbeEffect))!.DurationTicks.Should().Be(5);
        Service.GetStatusEffect(Target, typeof(LimitedDelayProbeEffect))!.DurationTicks.Should().Be(5);
    }

    [Test]
    public void APermanentApplicationOfNoType_Throws()
    {
        Service.Invoking(service => service.ApplyPermanentStatusEffect((Type)null!, Caster, Target))
            .Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void TheTypeOverloadsReadTheCatalog()
    {
        Service.DefinitionTypes = new[] { typeof(ProbeEffect), typeof(OtherProbeEffect) };
        Service.CacheData();

        Service.ApplyStatusEffect(Caster, Target, typeof(ProbeEffect), 5f).Should().BeTrue();
        Service.ApplyStatusEffect(Caster, Target, typeof(OtherProbeEffect), 5f, CombatDamageType.Physical).Should().BeTrue();

        Service.Invoking(service => service.ApplyStatusEffect(Caster, Target, typeof(ThirdProbeEffect), 5f))
            .Should().Throw<KeyNotFoundException>();
        Service.Invoking(service => service.ApplyStatusEffect(Caster, Target, (Type)null!, 5f, CombatDamageType.Physical))
            .Should().Throw<KeyNotFoundException>().WithMessage("*null*");
    }

    [Test]
    public void ADamageTypeOverloadUsesTheSourceResistanceWhenTheEffectHasNone()
    {
        Bench.Hostile.Add((Target, Caster));
        var effect = Probe<ProbeEffect>();

        Service.ApplyStatusEffect(Caster, Target, effect, 5f, CombatDamageType.Fire);

        effect.AppliedResistanceType.Should().Be(ResistanceType.Fire);
        Log.Should().Contain($"HasImmunity:{Target}:{ResistanceType.Fire}");
    }

    [Test]
    public void AnOverrideWinsOverTheEffectsOwnResistance()
    {
        Bench.Hostile.Add((Target, Caster));
        var effect = Probe<ProbeEffect>(probe => probe.ResistanceValue = ResistanceType.Mind);

        Service.ApplyStatusEffect(Caster, Target, effect, 5f, ResistanceType.Poison);

        effect.AppliedResistanceType.Should().Be(ResistanceType.Poison);
    }

    [Test]
    public void APhysicalSourceWithNoResistanceLeavesTheResistanceInvalid()
    {
        Bench.Hostile.Add((Target, Caster));
        var effect = Probe<ProbeEffect>();

        Service.ApplyStatusEffect(Caster, Target, effect, 5f, CombatDamageType.Invalid);

        effect.AppliedResistanceType.Should().Be(ResistanceType.Invalid);
        Log.Should().NotContain(line => line.StartsWith("HasImmunity"));
    }

    [Test]
    public void AnImmuneTarget_RefusesTheEffectAndTellsTheSource()
    {
        Bench.Hostile.Add((Target, Caster));
        Bench.Immune.Add((Target, ResistanceType.Mind));
        var effect = Probe<ProbeEffect>(probe => probe.ResistanceValue = ResistanceType.Mind);

        Service.ApplyStatusEffect(Caster, Target, effect, 5f).Should().BeFalse();

        Bench.Messages.Should().Equal((Caster, "Your ability was resisted."));
        Log.Should().NotContain(line => line.StartsWith("Apply:"));
        Log.Should().NotContain(line => line.StartsWith("CapstoneProof"));
    }

    [Test]
    public void AFullyResistedEffect_IsRefusedWithTheSameMessage()
    {
        Bench.Hostile.Add((Target, Caster));
        Bench.ResistedTicks = _ => 0;
        var effect = Probe<ProbeEffect>(probe => probe.ResistanceValue = ResistanceType.Mind);

        Service.ApplyStatusEffect(Caster, Target, effect, 5f).Should().BeFalse();

        Bench.Messages.Should().Equal((Caster, "Your ability was resisted."));
    }

    [TestCase(Invalid, Target)]
    [TestCase(Caster, Invalid)]
    public void AnInvalidSourceOrTarget_SkipsTheResistanceCheck(uint source, uint creature)
    {
        Bench.Hostile.Add((creature, source));
        var effect = Probe<ProbeEffect>(probe => probe.ResistanceValue = ResistanceType.Mind);

        Service.ApplyStatusEffect(source, creature, effect, 5f);

        Log.Should().NotContain(line => line.StartsWith("HasImmunity"));
    }

    [Test]
    public void AFriendlyTarget_SkipsTheResistanceCheck()
    {
        var effect = Probe<ProbeEffect>(probe => probe.ResistanceValue = ResistanceType.Mind);

        Service.ApplyStatusEffect(Caster, Target, effect, 5f);

        Log.Should().NotContain(line => line.StartsWith("HasImmunity"));
    }

    [Test]
    public void OutgoingDurationBonusesAreReadFromTheSourceBeforeResistance()
    {
        Service.Stats[(Caster, StatType.OutgoingDebuffDurationPercentAdjustment)] = 100;
        Service.Stats[(Caster, StatType.OutgoingBleedingDurationBonusSeconds)] = 4;
        var effect = Probe<ProbeEffect>(probe =>
            probe.CategoriesValue = StatusEffectCategory.Debuff | StatusEffectCategory.Bleeding);

        Service.ApplyStatusEffect(Caster, Target, effect, 10f);

        effect.DurationTicks.Should().Be(24);
    }

    [Test]
    public void ABleedWithoutABonusAndASourceThatIsGoneKeepTheirDuration()
    {
        var bleed = Probe<ProbeEffect>(probe => probe.CategoriesValue = StatusEffectCategory.Bleeding);
        Service.ApplyStatusEffect(Caster, Target, bleed, 10f);
        bleed.DurationTicks.Should().Be(10);

        Service.Stats[(Bystander, StatType.OutgoingDebuffDurationPercentAdjustment)] = 100;
        Bench.Invalid.Add(Bystander);
        var debuff = Probe<OtherProbeEffect>(probe => probe.CategoriesValue = StatusEffectCategory.Debuff);
        Service.ApplyStatusEffect(Bystander, Target, debuff, 10f);
        debuff.DurationTicks.Should().Be(10);
    }

    [Test]
    public void AForceDisruptionTakesTheSourcesForceDefenseAdjustment()
    {
        Service.Stats[(Caster, StatType.OutgoingForceDisruptionForceDefensePercentAdjustment)] = 7;
        var effect = Probe<ProbeEffect>(probe =>
        {
            probe.CategoriesValue = StatusEffectCategory.ForceDisruption;
            probe.StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = 3;
        });

        Service.ApplyStatusEffect(Caster, Target, effect, 10f);

        effect.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(10);
    }

    [Test]
    public void AForceDisruptionWithNoAdjustmentOrAnInvalidSourceIsUnchanged()
    {
        var effect = Probe<ProbeEffect>(probe => probe.CategoriesValue = StatusEffectCategory.ForceDisruption);
        Service.ApplyStatusEffect(Caster, Target, effect, 10f);
        effect.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(0);

        Service.Stats[(Bystander, StatType.OutgoingForceDisruptionForceDefensePercentAdjustment)] = 7;
        Bench.Invalid.Add(Bystander);
        var other = Probe<OtherProbeEffect>(probe => probe.CategoriesValue = StatusEffectCategory.ForceDisruption);
        Service.ApplyStatusEffect(Bystander, Target, other, 10f);
        other.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(0);
    }

    [Test]
    public void HardControlIsCappedAndCutFurtherBetweenPlayers()
    {
        var cc = HardControl();
        Service.ApplyStatusEffect(Caster, Target, cc, 100f);
        cc.DurationTicks.Should().Be(StatusEffectRules.MaximumHardCrowdControlDurationSeconds);

        Bench.PlayerAgainstPlayer = true;
        var pvp = Probe<OtherProbeEffect>(probe => probe.CategoriesValue = StatusEffectCategory.HardCrowdControl);
        Service.ApplyStatusEffect(Caster, Bystander, pvp, 100f);
        pvp.DurationTicks.Should().Be(15);
    }

    [Test]
    public void HardControlAppliedToOneselfIsNotCapped()
    {
        var cc = HardControl();

        Service.ApplyStatusEffect(Target, Target, cc, 100f);

        cc.DurationTicks.Should().Be(100);
    }

    [Test]
    public void AConversion_NeedsTheReplacedTypeToBeActiveAndReplacesItAsAReplacement()
    {
        var old = Applied<ProbeEffect>(Target, Caster, seconds: 6f);
        Service.DefinitionTypes = new[] { typeof(OtherProbeEffect) };
        Service.CacheData();

        Service.ApplyStatusEffect(Caster, Target, typeof(OtherProbeEffect), 30f, CombatDamageType.Invalid, typeof(ThirdProbeEffect))
            .Should().BeFalse("there is nothing of the replaced type to convert");
        Service.GetStatusEffect(Target, typeof(ProbeEffect)).Should().BeSameAs(old);

        Service.ApplyStatusEffect(Caster, Target, typeof(OtherProbeEffect), 30f, CombatDamageType.Invalid, typeof(ProbeEffect))
            .Should().BeTrue();

        Service.GetStatusEffect(Target, typeof(ProbeEffect)).Should().BeNull();
        Service.GetStatusEffect(Target, typeof(OtherProbeEffect))!.DurationTicks.Should().Be(30);
        Log.Should().Contain($"Remove:{old.Name}:{Target}:True");
    }

    public class HardConvertedEffect : ProbeEffect
    {
        public override StatusEffectCategory Categories => StatusEffectCategory.HardCrowdControl;
    }

    [Test]
    public void AConversionIsClampedToWhatRemainsOfTheOriginalControl()
    {
        var old = Applied<ProbeEffect>(Target, Caster, probe => probe.CategoriesValue = StatusEffectCategory.HardCrowdControl, 6f);
        Service.DefinitionTypes = new[] { typeof(HardConvertedEffect) };
        Service.CacheData();
        Now = Start.AddSeconds(2);

        Service.ApplyStatusEffect(Caster, Target, typeof(HardConvertedEffect), 30f, CombatDamageType.Invalid, typeof(ProbeEffect))
            .Should().BeTrue();

        Service.GetStatusEffect(Target, typeof(HardConvertedEffect))!.DurationTicks.Should().Be(4);
        old.GetType().Should().Be(typeof(ProbeEffect));
    }

    [Test]
    public void AConversionWithNoTimeLeftIsRefusedAndKeepsTheOriginal()
    {
        var old = Applied<ProbeEffect>(Target, Caster, probe => probe.CategoriesValue = StatusEffectCategory.HardCrowdControl, 6f);
        Service.DefinitionTypes = new[] { typeof(HardConvertedEffect) };
        Service.CacheData();
        Now = Start.AddSeconds(6);

        Service.ApplyStatusEffect(Caster, Target, typeof(HardConvertedEffect), 30f, CombatDamageType.Invalid, typeof(ProbeEffect))
            .Should().BeFalse();

        Service.GetStatusEffect(Target, typeof(ProbeEffect)).Should().BeSameAs(old);
    }

    [Test]
    public void ARefusedConversion_PutsTheOriginalBackForTheValidation()
    {
        var old = Applied<ProbeEffect>(Target, Caster, seconds: 6f);
        var seen = new List<int>();
        Service.DefinitionTypes = new[] { typeof(RefusingEffect) };
        Service.CacheData();
        RefusingEffect.OnCanApply = () => seen.Add(Service.GetCreatureStatusEffects(Target).GetAllEffects().Count);

        Service.ApplyStatusEffect(Caster, Target, typeof(RefusingEffect), 6f, CombatDamageType.Invalid, typeof(ProbeEffect))
            .Should().BeFalse();

        seen.Should().Equal(0);
        Service.GetCreatureStatusEffects(Target).GetAllEffects().Should().Equal(old);
        Bench.Messages.Single().Message.Should().Be("Effect failed to apply: Nope");
    }

    public class RefusingEffect : ProbeEffect
    {
        public static Action? OnCanApply { get; set; }

        public override string CanApply(uint creature)
        {
            OnCanApply?.Invoke();
            return "Nope";
        }
    }

    [Test]
    public void ARefusalGoesToASourcePlayerOrElseToTheCreature()
    {
        Bench.Players.Add(Caster);
        Service.ApplyStatusEffect(Caster, Target, Probe<ProbeEffect>(probe => probe.CanApplyAnswer = "Nope"), 5f)
            .Should().BeFalse();
        Bench.Messages.Last().Should().Be((Caster, "Effect failed to apply: Nope"));

        Bench.Players.Clear();
        Bench.Dms.Add(Caster);
        Service.ApplyStatusEffect(Caster, Target, Probe<ProbeEffect>(probe => probe.CanApplyAnswer = "Nope"), 5f);
        Bench.Messages.Last().Receiver.Should().Be(Caster);

        Bench.Dms.Clear();
        Service.ApplyStatusEffect(Caster, Target, Probe<ProbeEffect>(probe => probe.CanApplyAnswer = "Nope"), 5f);
        Bench.Messages.Last().Receiver.Should().Be(Target);

        Bench.Invalid.Add(Caster);
        Bench.Players.Add(Caster);
        Service.ApplyStatusEffect(Caster, Target, Probe<ProbeEffect>(probe => probe.CanApplyAnswer = "Nope"), 5f);
        Bench.Messages.Last().Receiver.Should().Be(Target);
    }

    [Test]
    public void AMorePowerfulActiveEffect_RefusesTheApplication()
    {
        Applied<OtherProbeEffect>(Target, Caster);
        var effect = Probe<ProbeEffect>();
        effect.MorePowerfulEffectTypes.Add(typeof(ThirdProbeEffect));
        effect.MorePowerfulEffectTypes.Add(typeof(OtherProbeEffect));

        Service.ApplyStatusEffect(Caster, Target, effect, 5f).Should().BeFalse();

        Bench.Messages.Single().Message.Should().Be("A more powerful effect is active.");
    }

    [Test]
    public void ALessPowerfulActiveEffect_IsReplacedQuietly()
    {
        var weak = Applied<OtherProbeEffect>(Target, Caster);
        var effect = Probe<ProbeEffect>();
        effect.LessPowerfulEffectTypes.Add(typeof(OtherProbeEffect));

        Service.ApplyStatusEffect(Caster, Target, effect, 5f).Should().BeTrue();

        Log.Should().ContainInOrder($"Remove:{weak.Name}:{Target}:True", $"Apply:{effect.Name}:{Target}:5");
        Log.Should().NotContain(line => line.Contains("worn off"));
        Service.GetStatusEffect(Target, typeof(OtherProbeEffect)).Should().BeNull();
    }

    [TestCase(StatusEffectStackType.Disabled, 1)]
    [TestCase(StatusEffectStackType.Invalid, 1)]
    [TestCase(StatusEffectStackType.StackFromMultipleSources, 2)]
    [TestCase(StatusEffectStackType.UnlimitedStacking, 3)]
    public void TheStackingTypeDecidesWhatANewApplicationReplaces(StatusEffectStackType stacking, int expected)
    {
        Applied<ProbeEffect>(Target, Caster, probe => probe.StackValue = stacking);
        Applied<ProbeEffect>(Target, Bystander, probe => probe.StackValue = stacking);

        Service.ApplyStatusEffect(Caster, Target, Probe<ProbeEffect>(probe => probe.StackValue = stacking), 5f);

        if (stacking == StatusEffectStackType.Disabled || stacking == StatusEffectStackType.Invalid)
            Service.GetCreatureStatusEffects(Target).GetAllEffects().Should().ContainSingle();
        else
            Service.GetCreatureStatusEffects(Target).GetAllEffects().Should().HaveCount(expected);
    }

    [Test]
    public void AStatusThatKeepsItsTickSchedule_AnchorsToTheEarliestTickOfWhatItReplaces()
    {
        var old = Applied<ProbeEffect>(Target, Caster, probe => probe.PreservesTicks = true, 6f);
        Now = Start.AddSeconds(3);
        var fresh = Probe<ProbeEffect>(probe => probe.PreservesTicks = true);

        Service.ApplyStatusEffect(Caster, Target, fresh, 6f);

        fresh.LastTickTime.Should().Be(old.LastTickTime);
    }

    [Test]
    public void GetRefreshTickAnchor_FollowsTheStackingRules()
    {
        var incoming = Probe<ProbeEffect>(probe => probe.PreservesTicks = true);
        Service.GetRefreshTickAnchor(incoming, Array.Empty<IStatusEffect>(), Caster).Should().BeNull();

        var active = Probe<ProbeEffect>();
        active.ApplyEffect(Bystander, Target, 5);
        Service.GetRefreshTickAnchor(incoming, new IStatusEffect[] { active }, Caster).Should().Be(active.LastTickTime);

        incoming.StackValue = StatusEffectStackType.StackFromMultipleSources;
        Service.GetRefreshTickAnchor(incoming, new IStatusEffect[] { active }, Caster).Should().BeNull("only the same source shares a schedule");
        Service.GetRefreshTickAnchor(incoming, new IStatusEffect[] { active }, Bystander).Should().Be(active.LastTickTime);

        incoming.StackValue = StatusEffectStackType.UnlimitedStacking;
        Service.GetRefreshTickAnchor(incoming, new IStatusEffect[] { active }, Bystander).Should().BeNull();

        incoming.StackValue = StatusEffectStackType.Disabled;
        incoming.PreservesTicks = false;
        Service.GetRefreshTickAnchor(incoming, new IStatusEffect[] { active }, Bystander).Should().BeNull();

        incoming.PreservesTicks = true;
        active.Flag();
        Service.GetRefreshTickAnchor(incoming, new IStatusEffect[] { active }, Bystander).Should().BeNull();
    }

    [Test]
    public void AnEffectThatFlagsItselfDuringApply_IsTakenBackOffAndLeavesNoEmptyTracker()
    {
        var effect = Probe<ProbeEffect>(probe => probe.FlagOnApply = true);

        Service.ApplyStatusEffect(Caster, Target, effect, 5f).Should().BeFalse();

        Log.Should().ContainInOrder($"Apply:{effect.Name}:{Target}:5", $"Remove:{effect.Name}:{Target}:False");
        Service.GetCreatureStatusEffects(Target).GetAllEffects().Should().BeEmpty();
        Bench.Engine.CallsTo("RemoveEffectByTag").Should().Contain(call => ((string[])call.Arguments[1]!).Contains("STATUS_EFFECT"));
    }

    [Test]
    public void AStackingEffectThatFlagsItselfDuringApply_AlsoLeavesNoEmptyTracker()
    {
        var effect = Probe<ProbeEffect>(probe =>
        {
            probe.StackValue = StatusEffectStackType.UnlimitedStacking;
            probe.FlagOnApply = true;
        });

        Service.ApplyStatusEffect(Caster, Target, effect, 5f).Should().BeFalse();

        Log.Should().ContainInOrder($"Apply:{effect.Name}:{Target}:5", $"Remove:{effect.Name}:{Target}:False");
        Bench.Engine.CallsTo("RemoveEffectByTag")
            .Should().ContainSingle(call => ((string[])call.Arguments[1]!).Contains("STATUS_EFFECT"));
    }

    [Test]
    public void AnEffectThatFlagsItselfBesideOthers_KeepsTheTracker()
    {
        var other = Applied<OtherProbeEffect>(Target, Caster);

        Service.ApplyStatusEffect(Caster, Target, Probe<ProbeEffect>(probe => probe.FlagOnApply = true), 5f).Should().BeFalse();

        Service.GetCreatureStatusEffects(Target).GetAllEffects().Should().Equal(other);
    }

    [Test]
    public void AFriendlyOrSelfAppliedDebuff_GrantsNoHostilePartyBuff()
    {
        Service.ApplyStatusEffect(Caster, Target, Probe<ProbeEffect>(probe => probe.CategoriesValue = StatusEffectCategory.Debuff), 5f);
        Log.Should().NotContain(line => line.StartsWith("HostileBuff"));

        Bench.Hostile.Add((Caster, Caster));
        Service.ApplyStatusEffect(Caster, Caster, Probe<ProbeEffect>(probe => probe.CategoriesValue = StatusEffectCategory.Debuff), 5f);
        Log.Should().NotContain(line => line.StartsWith("HostileBuff"));
    }

    [Test]
    public void AResistanceMessageGoesOnlyToAPlayerSource()
    {
        Bench.Hostile.Add((Target, Caster));
        Bench.ResistedTicks = ticks => ticks / 2;

        Service.ApplyStatusEffect(Caster, Target, Probe<ProbeEffect>(probe => probe.ResistanceValue = ResistanceType.Mind), 10f);
        Bench.Messages.Should().BeEmpty();

        Bench.Players.Add(Caster);
        Service.ApplyStatusEffect(Caster, Target, Probe<ProbeEffect>(probe => probe.ResistanceValue = ResistanceType.Mind), 10f);
        Bench.Messages.Single().Should().Be((Caster, "Mind Resistance reduced Probe duration from 10s to 5s."));

        Bench.Players.Clear();
        Bench.Dms.Add(Caster);
        Service.ApplyStatusEffect(Caster, Target, Probe<ProbeEffect>(probe => probe.ResistanceValue = ResistanceType.Mind), 10f);
        Bench.Messages.Should().HaveCount(2);
    }

    [Test]
    public void VulnerabilityExtendsTheDurationWithoutRoundingAShortBonusUp()
    {
        Bench.Hostile.Add((Target, Caster));
        Bench.ResistedTicks = ticks => ticks + 5;

        var effect = Probe<ProbeEffect>(probe => probe.ResistanceValue = ResistanceType.Mind);
        Service.ApplyStatusEffect(Caster, Target, effect, 10f);

        effect.DurationTicks.Should().Be(15);
    }

    [Test]
    public void ASilentEffectSendsNoApplicationMessage()
    {
        Service.ApplyStatusEffect(Caster, Target, Probe<ProbeEffect>(probe => probe.SendsApplication = false), 5f);

        Log.Should().NotContain(line => line.StartsWith("Nearby"));
        Bench.Messages.Should().BeEmpty();
    }

    [Test]
    public void AnApplicationWithNoOtherSource_NamesOnlyTheBearer()
    {
        Service.ApplyStatusEffect(Target, Target, Probe<ProbeEffect>(), 5f);

        Log.Should().Contain($"Nearby:{Target}:name-{Target} receives the effect of Probe");
    }

    [Test]
    public void ASourceWithoutADisplayName_IsLeftOutOfTheMessage()
    {
        var names = new Dictionary<uint, string> { { Caster, " " } };
        Bench.DisplayNames = names;

        Service.ApplyStatusEffect(Caster, Target, Probe<ProbeEffect>(), 5f);

        Log.Should().Contain($"Nearby:{Target}:name-{Target} receives the effect of Probe");
    }

    [Test]
    public void AHiddenBearer_IsToldAloneAndAPlayerSourceHearsSeparately()
    {
        Bench.Concealed.Add(Target);
        Bench.Players.Add(Caster);

        Service.ApplyStatusEffect(Caster, Target, Probe<ProbeEffect>(), 5f);

        Log.Should().NotContain(line => line.StartsWith("Nearby"));
        Bench.Messages.Should().Equal(
            (Target, $"name-{Target} receives the effect of Probe from name-{Caster}"),
            (Caster, $"name-{Target} receives the effect of Probe from name-{Caster}"));
    }

    [Test]
    public void AHiddenSource_KeepsTheAnnouncementFromTheNeighbourhood()
    {
        Bench.Concealed.Add(Caster);

        Service.ApplyStatusEffect(Caster, Target, Probe<ProbeEffect>(), 5f);

        Log.Should().NotContain(line => line.StartsWith("Nearby"));
        Bench.Messages.Should().ContainSingle(message => message.Receiver == Target);
    }

    [TestCase(false, false, true)]
    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    [TestCase(true, true, false)]
    public void ShouldAnnounceNearby_NeedsNeitherToBeHidden(bool bearerHidden, bool sourceHidden, bool expected)
    {
        Service.ShouldAnnounceNearby(bearerHidden, sourceHidden).Should().Be(expected);
    }

    [Test]
    public void ALeadershipEffect_KeepsOnlyTheStrongestValuePerStat()
    {
        var weak = Probe<LeadershipProbeEffect>(probe =>
        {
            probe.Nominal[StatType.PhysicalDefense] = -10;
            probe.Nominal[StatType.ForceDefense] = -40;
            probe.StatGroup.Stats[StatType.PhysicalDefense] = -10;
            probe.StatGroup.Stats[StatType.ForceDefense] = -40;
        });
        var strong = Probe<OtherLeadershipProbeEffect>(probe =>
        {
            probe.Nominal[StatType.PhysicalDefense] = -25;
            probe.StatGroup.Stats[StatType.PhysicalDefense] = -25;
        });

        Service.ApplyStatusEffect(Caster, Target, weak, 0f);
        Service.ApplyStatusEffect(Caster, Target, strong, 0f);

        weak.StatGroup.Stats[StatType.PhysicalDefense].Should().Be(0, "the stronger effect wins this channel");
        weak.StatGroup.Stats[StatType.ForceDefense].Should().Be(-40, "nothing competes for this one");
        strong.StatGroup.Stats[StatType.PhysicalDefense].Should().Be(-25);
        Service.GetStatAdjustment(Target, StatType.PhysicalDefense).Should().Be(-25);
        Service.GetStatAdjustment(Target, StatType.ForceDefense).Should().Be(-40);

        Service.RemoveStatusEffect(typeof(OtherLeadershipProbeEffect), Target);

        weak.StatGroup.Stats[StatType.PhysicalDefense].Should().Be(-10, "removal recomputes every channel");
        Service.GetStatAdjustment(Target, StatType.PhysicalDefense).Should().Be(-10);
    }

    [Test]
    public void TheLastLeadershipEffectRemoved_LeavesNothingToReconcile()
    {
        Service.ApplyStatusEffect(Caster, Target, Probe<LeadershipProbeEffect>(probe =>
        {
            probe.Nominal[StatType.PhysicalDefense] = -10;
            probe.StatGroup.Stats[StatType.PhysicalDefense] = -10;
        }), 0f);

        Service.RemoveStatusEffect(typeof(LeadershipProbeEffect), Target);

        Service.GetStatAdjustment(Target, StatType.PhysicalDefense).Should().Be(0);
    }

    [Test]
    public void AVitalityEffect_SchedulesTheTemporaryHitPointCheckOnApplyAndRemove()
    {
        var effect = Probe<ProbeEffect>(probe => probe.StatGroup.Abilities[AbilityType.Vitality] = 2);
        Service.ApplyStatusEffect(Caster, Target, effect, 0f);
        Log.Should().Contain($"LimitCheck:{Target}");

        Log.Clear();
        Service.RemoveStatusEffect(typeof(ProbeEffect), Target);
        Log.Should().Contain($"LimitCheck:{Target}");

        Log.Clear();
        Service.ApplyStatusEffect(Caster, Target, Probe<OtherProbeEffect>(), 0f);
        Log.Should().NotContain($"LimitCheck:{Target}");
    }

    [Test]
    public void AbilityAdjustmentsBecomeNativeIncreasesAndDecreasesSkippingZeros()
    {
        var effect = Probe<ProbeEffect>(probe =>
        {
            probe.StatGroup.Abilities[AbilityType.Might] = 3;
            probe.StatGroup.Abilities[AbilityType.Social] = -2;
            probe.StatGroup.Abilities[AbilityType.Invalid] = 9;
        });

        Service.ApplyStatusEffect(Caster, Target, effect, 0f);

        Bench.Engine.CallsTo("EffectAbilityIncrease").Should().ContainSingle(call => (AbilityType)call.Arguments[0]! == AbilityType.Might && (int)call.Arguments[1]! == 3);
        Bench.Engine.CallsTo("EffectAbilityDecrease").Should().ContainSingle(call => (AbilityType)call.Arguments[0]! == AbilityType.Social && (int)call.Arguments[1]! == 2);
        Bench.Engine.CountOf("EffectLinkEffects").Should().BeGreaterThanOrEqualTo(2);
    }

    [Test]
    public void ANativeEffectThatAlreadyExists_IsNotAppliedTwice()
    {
        var effect = Probe<ProbeEffect>();
        Bench.NativeTags.Add(effect.Id);

        Service.ApplyStatusEffect(Caster, Target, effect, 5f);

        Bench.Engine.CallsTo("ApplyEffectToObject").Should().BeEmpty();
    }

    [Test]
    public void AnEffectApplied_ItsOwnOriginIsKeptOverTheActiveAbility()
    {
        var parent = new AbilityDetail { Name = "Parent" };
        Service.ActiveImpact = new AbilityImpactSummary { Ability = ActiveAbility };
        var effect = Probe<ProbeEffect>(probe => probe.OriginatingAbility = parent);

        Service.ApplyStatusEffect(Caster, Target, effect, 5f);

        effect.OriginatingAbility.Should().BeSameAs(parent);
        Log.Should().Contain($"CapstoneProof:{Caster}:{Target}:ProbeEffect:Parent");
    }

    [Test]
    public void ARefreshInPlace_RestartsTheActiveEffectWithoutRemovingItOrRepeatingItsMessage()
    {
        Bench.Hostile.Add((Target, Bystander));
        var active = Applied<ProbeEffect>(Target, Caster, probe => probe.RefreshAnswer = _ => true, 5f);
        var incoming = Probe<ProbeEffect>(probe => probe.RefreshAnswer = _ => true);
        Now = Start.AddSeconds(2);
        Bench.Engine.ClearCalls();

        Service.ApplyStatusEffect(Bystander, Target, incoming, 8f).Should().BeTrue();

        Service.GetCreatureStatusEffects(Target).GetAllEffects().Should().Equal(active);
        active.Source.Should().Be(Bystander, "a non-stacking effect moves to the new source");
        active.DurationTicks.Should().Be(8);
        Log.Should().NotContain(line => line.StartsWith("Apply:"));
        Log.Should().NotContain(line => line.StartsWith("Remove:"));
        Log.Should().NotContain(line => line.StartsWith("Nearby"));
        Log.Should().Contain($"Refresh:{Target}:StatusEffectReceivedRefreshEvent");
        Log.Should().Contain($"CapstoneProof:{Bystander}:{Target}:ProbeEffect:");
        Bench.Engine.CountOf("ApplyEffectToObject").Should().Be(1);
    }

    [Test]
    public void ARefreshInPlace_FromTheSameSourceKeepsTheSourceAndRemovesLessPowerfulEffects()
    {
        var active = Applied<ProbeEffect>(Target, Caster, probe => probe.RefreshAnswer = _ => true, 5f);
        var weak = Applied<OtherProbeEffect>(Target, Caster);
        var incoming = Probe<ProbeEffect>(probe => probe.RefreshAnswer = _ => true);
        incoming.LessPowerfulEffectTypes.Add(typeof(OtherProbeEffect));

        Service.ApplyStatusEffect(Caster, Target, incoming, 8f).Should().BeTrue();

        active.Source.Should().Be(Caster);
        Log.Should().Contain($"Remove:{weak.Name}:{Target}:True");
    }

    [Test]
    public void ARefreshInPlace_ThatAHostileAbilityMade_RunsTheOnApplicationCombatTriggers()
    {
        Bench.Hostile.Add((Target, Caster));
        Bench.Players.Add(Caster);
        Bench.ResistedTicks = ticks => ticks / 2;
        var active = Applied<ProbeEffect>(Target, Caster, probe =>
        {
            probe.RefreshAnswer = _ => true;
            probe.CategoriesValue = StatusEffectCategory.Debuff;
            probe.ResistanceValue = ResistanceType.Mind;
        }, 5f);
        active.AssignResistanceType(ResistanceType.Mind);
        var incoming = Probe<ProbeEffect>(probe =>
        {
            probe.RefreshAnswer = _ => true;
            probe.CategoriesValue = StatusEffectCategory.Debuff;
            probe.ResistanceValue = ResistanceType.Mind;
        });

        Bench.Recording = true;
        Service.ApplyStatusEffect(Caster, Target, incoming, 8f).Should().BeTrue();

        Log.Should().ContainInOrder(
            $"RecordStatusApplied:{Caster}:{Target}:Probe:False:True",
            $"StaminaDrain:{Caster}:{Target}:{StatusEffectCategory.Debuff}",
            $"HostileBuff:{Caster}::0:True");
        Bench.Messages.Single().Message.Should().Be("Mind Resistance reduced Probe duration from 8s to 4s.");
    }

    [TestCase(StatusEffectStackType.UnlimitedStacking)]
    [TestCase(StatusEffectStackType.Invalid)]
    public void ARefreshInPlace_NeedsAStackingTypeThatReplaces(StatusEffectStackType stacking)
    {
        var active = Applied<ProbeEffect>(Target, Caster, probe =>
        {
            probe.RefreshAnswer = _ => true;
            probe.StackValue = stacking;
        }, 5f);

        Service.ApplyStatusEffect(Caster, Target, Probe<ProbeEffect>(probe =>
        {
            probe.RefreshAnswer = _ => true;
            probe.StackValue = stacking;
        }), 8f).Should().BeTrue();

        Log.Should().Contain(line => line.StartsWith("Apply:"));
        active.DurationTicks.Should().Be(5);
    }

    [Test]
    public void ARefreshInPlace_NeedsTheCreatureToBeTrackedAndTheEffectToOptIn()
    {
        Service.ApplyStatusEffect(Caster, Target, Probe<ProbeEffect>(probe => probe.RefreshAnswer = _ => true), 5f).Should().BeTrue();
        Log.Should().Contain(line => line.StartsWith("Apply:"), "nothing was active to refresh");

        Log.Clear();
        Service.ApplyStatusEffect(Caster, Target, Probe<ProbeEffect>(probe => probe.RefreshAnswer = _ => false), 5f).Should().BeTrue();
        Log.Should().Contain(line => line.StartsWith("Apply:"), "the incoming effect did not opt in");
    }

    [Test]
    public void ARefreshInPlace_OfSeveralInstancesReplacesThemAll()
    {
        Applied<ProbeEffect>(Target, Caster, probe => probe.RefreshAnswer = _ => true, 5f);
        Applied<ProbeEffect>(Target, Bystander, probe => probe.RefreshAnswer = _ => true, 5f);

        Service.ApplyStatusEffect(Caster, Target, Probe<ProbeEffect>(probe => probe.RefreshAnswer = _ => true), 5f);

        Log.Should().Contain(line => line.StartsWith("Apply:"));
        Service.GetCreatureStatusEffects(Target).GetAllEffects().Should().ContainSingle();
    }

    [Test]
    public void ARefreshInPlace_FromAnotherSourceOfAStackingEffectAddsAnInstance()
    {
        Applied<ProbeEffect>(Target, Bystander, probe =>
        {
            probe.RefreshAnswer = _ => true;
            probe.StackValue = StatusEffectStackType.StackFromMultipleSources;
        }, 5f);

        Service.ApplyStatusEffect(Caster, Target, Probe<ProbeEffect>(probe =>
        {
            probe.RefreshAnswer = _ => true;
            probe.StackValue = StatusEffectStackType.StackFromMultipleSources;
        }), 5f);

        Service.GetCreatureStatusEffects(Target).GetAllEffects().Should().HaveCount(2);
    }

    private static readonly object[] RefreshRefusals =
    {
        new object[] { "flagged" },
        new object[] { "permanent" },
        new object[] { "resistance" },
        new object[] { "origin" },
        new object[] { "stats" },
        new object[] { "active-refuses" },
    };

    [TestCaseSource(nameof(RefreshRefusals))]
    public void ARefreshInPlace_FallsBackToAFreshApplicationWhenTheStateWouldDiffer(string difference)
    {
        var active = Applied<ProbeEffect>(Target, Caster, probe => probe.RefreshAnswer = _ => true, difference == "permanent" ? 0f : 5f);
        var incoming = Probe<ProbeEffect>(probe => probe.RefreshAnswer = _ => true);
        switch (difference)
        {
            case "flagged":
                active.Flag();
                break;
            case "resistance":
                active.AssignResistanceType(ResistanceType.Mind);
                break;
            case "origin":
                active.OriginatingAbility = new AbilityDetail();
                break;
            case "stats":
                incoming.StatGroup.Stats[StatType.Attack] = 5;
                break;
            case "active-refuses":
                incoming.RefreshAnswer = other => !ReferenceEquals(other, active) || refreshes-- > 0;
                break;
        }

        Service.ApplyStatusEffect(Caster, Target, incoming, 8f).Should().BeTrue();

        Log.Should().Contain(line => line.StartsWith("Apply:"));
        Service.GetCreatureStatusEffects(Target).GetAllEffects().Should().Equal(incoming);
    }

    private int refreshes = 1;

    [Test]
    public void ALimitedAttackEffect_WaitsForTheNativeCycleToEndAndIsNotProfiledTwice()
    {
        Service.BeginNativeAttackSwing(Target);
        Service.BeginNativeAttackSwing(Target);
        var effect = Probe<LimitedDelayProbeEffect>();
        var noDelay = Probe<LimitedNoDelayProbeEffect>();

        Service.ApplyStatusEffect(Caster, Target, effect, 10f).Should().BeTrue();
        Service.ApplyStatusEffect(Caster, Target, noDelay, 10f).Should().BeTrue();

        Service.GetCreatureStatusEffects(Target).GetAllEffects().Should().BeEmpty();
        Log.Should().Equal($"CapstoneProof:{Caster}:{Target}:LimitedDelayProbeEffect:", $"CapstoneProof:{Caster}:{Target}:LimitedNoDelayProbeEffect:");

        Service.EndNativeAttackSwing(Target);
        Service.GetCreatureStatusEffects(Target).GetAllEffects().Should().BeEmpty("the outer cycle is still open");

        Service.EndNativeAttackSwing(Target);
        Service.GetCreatureStatusEffects(Target).GetAllEffects().Should().BeEquivalentTo(new IStatusEffect[] { effect, noDelay });
        Log.Should().Contain($"Apply:Probe:{Target}:10");
    }

    [Test]
    public void AnOrdinaryEffectDuringANativeCycleAppliesAtOnce()
    {
        Service.BeginNativeAttackSwing(Target);

        Service.ApplyStatusEffect(Caster, Target, Probe<ProbeEffect>(), 10f).Should().BeTrue();

        Service.GetCreatureStatusEffects(Target).GetAllEffects().Should().ContainSingle();
        Service.EndNativeAttackSwing(Target);
    }

    [Test]
    public void ALimitedAttackEffectOutsideANativeCycleAppliesAtOnce()
    {
        Service.ApplyStatusEffect(Caster, Target, Probe<LimitedNoDelayProbeEffect>(), 10f).Should().BeTrue();

        Service.GetCreatureStatusEffects(Target).GetAllEffects().Should().ContainSingle();
    }

    [Test]
    public void EndingACycleThatWasNeverStartedDoesNothing()
    {
        Service.EndNativeAttackSwing(Target);

        Service.GetCreatureStatusEffects(Target).GetAllEffects().Should().BeEmpty();
    }

    [Test]
    public void ADestroyedAttacker_LosesItsOpenCycleAndDeferredEffects()
    {
        Service.BeginNativeAttackSwing(Target);
        Service.ApplyStatusEffect(Caster, Target, Probe<LimitedNoDelayProbeEffect>(), 10f);
        Log.Clear();

        Service.ForgetDestroyedNativeAttackSwing(Target);
        Service.EndNativeAttackSwing(Target);

        Log.Should().BeEmpty();
        Service.GetCreatureStatusEffects(Target).GetAllEffects().Should().BeEmpty();
    }
}
