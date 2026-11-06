using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>The readers, and every way a status comes off a creature.</summary>
[TestFixture]
public class StatusEffectServiceRemovalTests : StatusEffectServiceTestBase
{
    [Test]
    public void TheCreatureReaders_AnswerEmptyForAnUntrackedCreature()
    {
        Service.GetCreatureStatusEffects(Target).GetAllEffects().Should().BeEmpty();
        Service.GetStatSources(Target, StatType.Attack).Should().BeEmpty();
        Service.GetStatAdjustment(Target, StatType.Attack).Should().Be(0);
        Service.HasAnyActiveEffect(Target, new HashSet<Type> { typeof(ProbeEffect) }).Should().BeFalse();
        Service.HasAnyActiveEffect(Target, StatusEffectSourceType.Normal).Should().BeFalse();
        Service.HasStatusEffect(Target, typeof(ProbeEffect)).Should().BeFalse();
        Service.HasStatusEffect<ProbeEffect>(Target).Should().BeFalse();
        Service.HasStatusEffect(Target, typeof(ProbeEffect), Caster).Should().BeFalse();
        Service.HasStatusEffect(Target, typeof(ProbeEffect), typeof(OtherProbeEffect)).Should().BeFalse();
        Service.HasStatusEffectCategory(Target, StatusEffectCategory.Debuff).Should().BeFalse();
        Service.GetStatusEffect(Target, typeof(ProbeEffect)).Should().BeNull();
        Service.GetStatusEffect(Target, typeof(ProbeEffect), Caster).Should().BeNull();
        Service.GetStatusEffect<ProbeEffect>(Target).Should().BeNull();
        Service.HasEffect(typeof(ProbeEffect), Target).Should().BeFalse();
        Service.HasEffect<ProbeEffect>(Target).Should().BeFalse();
        Service.HasCleanseableStatusEffect(Target, StatusEffectCleanseType.Purify).Should().BeFalse();
        Service.GetStatusEffectSourceWithStat(Target, StatType.Attack).Should().Be(Invalid);
    }

    [Test]
    public void TheCreatureReaders_SeeWhatIsActive()
    {
        var effect = Applied<ProbeEffect>(Target, Caster, probe =>
        {
            probe.CategoriesValue = StatusEffectCategory.Debuff;
            probe.CleanseValue = StatusEffectCleanseType.Purify;
            probe.StatGroup.Stats[StatType.Attack] = 4;
        });

        Service.GetStatSources(Target, StatType.Attack).Should().ContainSingle();
        Service.GetStatAdjustment(Target, StatType.Attack).Should().Be(4);
        Service.HasAnyActiveEffect(Target, new HashSet<Type> { typeof(ProbeEffect) }).Should().BeTrue();
        Service.HasAnyActiveEffect(Target, new HashSet<Type> { typeof(OtherProbeEffect) }).Should().BeFalse();
        Service.HasAnyActiveEffect(Target, StatusEffectSourceType.Normal).Should().BeTrue();
        Service.HasStatusEffect(Target, typeof(ProbeEffect)).Should().BeTrue();
        Service.HasStatusEffect<ProbeEffect>(Target).Should().BeTrue();
        Service.HasStatusEffect(Target, typeof(ProbeEffect), Caster).Should().BeTrue();
        Service.HasStatusEffect(Target, typeof(ProbeEffect), Bystander).Should().BeFalse();
        Service.HasStatusEffect(Target, typeof(OtherProbeEffect), typeof(ProbeEffect)).Should().BeTrue();
        Service.HasStatusEffectCategory(Target, StatusEffectCategory.Debuff).Should().BeTrue();
        Service.HasStatusEffectCategory(Target, StatusEffectCategory.Buff).Should().BeFalse();
        Service.GetStatusEffect(Target, typeof(ProbeEffect)).Should().BeSameAs(effect);
        Service.GetStatusEffect(Target, typeof(OtherProbeEffect)).Should().BeNull();
        Service.GetStatusEffect(Target, typeof(ProbeEffect), Caster).Should().BeSameAs(effect);
        Service.GetStatusEffect(Target, typeof(ProbeEffect), Bystander).Should().BeNull();
        Service.GetStatusEffect<ProbeEffect>(Target).Should().BeSameAs(effect);
        Service.HasEffect(typeof(ProbeEffect), Target).Should().BeTrue();
        Service.HasEffect<ProbeEffect>(Target).Should().BeTrue();
        Service.HasEffect<OtherProbeEffect>(Target).Should().BeFalse();
        Service.HasCleanseableStatusEffect(Target, StatusEffectCleanseType.Purify).Should().BeTrue();
        Service.HasCleanseableStatusEffect(Target, StatusEffectCleanseType.TreatmentKit1).Should().BeFalse();
        Service.HasCleanseType(effect, StatusEffectCleanseType.Purify).Should().BeTrue();
    }

    [Test]
    public void TheStatSourceIsTheValidSourceWithTheLargestContribution()
    {
        Applied<ProbeEffect>(Target, Caster, probe => probe.StatGroup.Stats[StatType.Attack] = 3);
        Applied<OtherProbeEffect>(Target, Bystander, probe => probe.StatGroup.Stats[StatType.Attack] = -5);
        Applied<ThirdProbeEffect>(Target, 999, probe => probe.StatGroup.Stats[StatType.Attack] = 50);
        Bench.Invalid.Add(999);

        Service.GetStatusEffectSourceWithStat(Target, StatType.Attack).Should().Be(Bystander);
        Service.GetStatusEffectSourceWithStat(Target, StatType.Defense).Should().Be(Invalid);
    }

    [Test]
    public void ConsumeAndSetStatus_ChangeTheStatTotal()
    {
        var effect = Applied<ProbeEffect>(Target, Caster, probe => probe.StatGroup.Stats[StatType.Attack] = 3);

        Service.SetStatusEffectStat(Target, effect, StatType.Attack, 8);
        Service.GetStatAdjustment(Target, StatType.Attack).Should().Be(8);
        Log.Should().ContainSingle(line => line == $"Refresh:{Target}:StatAdjustmentRefreshEvent");

        Log.Clear();
        Service.SetStatusEffectStat(Target, effect, StatType.Attack, 8);
        Log.Should().BeEmpty("nothing changed");

        Service.ConsumeStatusEffectStat(Target, StatType.Attack);
        Service.GetStatAdjustment(Target, StatType.Attack).Should().Be(0);
    }

    [Test]
    public void Removing_RunsEveryStepInTheOriginalOrder()
    {
        var effect = Applied<HandlerProbeEffect>(Target, Caster, probe =>
        {
            probe.SourceValue = StatusEffectSourceType.Stance;
            probe.StatGroup.Stats[StatType.Attack] = 2;
            probe.StatGroup.Abilities[SWLOR.NWN.API.NWScript.Enum.AbilityType.Vitality] = 1;
        });

        Service.RemoveStatusEffect(Target, typeof(HandlerProbeEffect));

        Log.Should().Equal(
            $"Nearby:{Target}:name-{Target}'s Probe effect has worn off.",
            $"Remove:Probe:{Target}:False",
            $"OnStanceChanged:{Target}",
            $"AfterRemoved:Probe:{Target}",
            $"LimitCheck:{Target}",
            $"Refresh:{Target}:StatusEffectRemovedRefreshEvent");

        Service.HasEffect(typeof(HandlerProbeEffect), Target).Should().BeFalse();
        Bench.Engine.CallsTo("RemoveEffectByTag").Select(call => ((string[])call.Arguments[1]!).Single())
            .Should().Equal(effect.Id, "STATUS_EFFECT");
        var delayed = Bench.Engine.CallsTo("DelayCommand").Single();
        delayed.Arguments[0].Should().Be(0.1f);

        Log.Clear();
        Bench.RunDeferred("DelayCommand");
        Log.Should().Equal($"ApplyCreatureMovementRate:{Target}");
    }

    [Test]
    public void Removing_FromAHiddenCreatureTellsOnlyThatCreature()
    {
        Applied<ProbeEffect>(Target, Caster);
        Bench.Concealed.Add(Target);

        Service.RemoveStatusEffect(Target, typeof(ProbeEffect));

        Log.Should().NotContain(line => line.StartsWith("Nearby"));
        Bench.Messages.Should().Equal((Target, $"name-{Target}'s Probe effect has worn off."));
    }

    [Test]
    public void OnlyTheLastInstanceOfAStackSendsTheMessage()
    {
        Applied<ProbeEffect>(Target, Caster, probe => probe.StackValue = StatusEffectStackType.UnlimitedStacking);
        Applied<ProbeEffect>(Target, Caster, probe => probe.StackValue = StatusEffectStackType.UnlimitedStacking);
        Applied<ProbeEffect>(Target, Bystander, probe => probe.StackValue = StatusEffectStackType.UnlimitedStacking);

        Service.RemoveStatusEffect(Target, typeof(ProbeEffect), Caster);
        Log.Should().NotContain(line => line.StartsWith("Nearby"));

        Service.RemoveStatusEffect(typeof(ProbeEffect), Target);
        Log.Should().ContainSingle(line => line.StartsWith("Nearby"));
    }

    [Test]
    public void TheRemovalOptions_SilenceTheMessageAndLeaveTheNativeEffect()
    {
        Applied<ProbeEffect>(Target, Caster);
        Service.RemoveStatusEffect(Target, typeof(ProbeEffect), false, false);
        Log.Should().NotContain(line => line.StartsWith("Nearby"));
        Bench.Engine.CallsTo("RemoveEffectByTag").Should().BeEmpty();

        Applied<OtherProbeEffect>(Target, Caster, probe => probe.SendsWornOff = false);
        Service.RemoveStatusEffect(Target, typeof(OtherProbeEffect), Caster);
        Log.Should().NotContain(line => line.StartsWith("Nearby"));
        Bench.Engine.CallsTo("RemoveEffectByTag").Should().NotBeEmpty();
    }

    [Test]
    public void RemovingFromAnUntrackedCreatureDoesNothing()
    {
        Service.RemoveStatusEffect<ProbeEffect>(Target);
        Service.RemoveStatusEffect(typeof(ProbeEffect), Target);
        Service.RemoveAllStatusEffects(Target);

        Log.Should().BeEmpty();
        Bench.Engine.CallsTo("RemoveEffectByTag").Should().BeEmpty();
    }

    [Test]
    public void RemoveAll_TakesCraftingStylesOnlyWhenAsked()
    {
        Applied<ProbeEffect>(Target, Caster);
        var style = Applied<OtherProbeEffect>(Target, Caster, probe => probe.SourceValue = StatusEffectSourceType.CraftingStyle);

        Service.RemoveAllStatusEffects(Target);
        Service.GetCreatureStatusEffects(Target).GetAllEffects().Should().Equal(style);

        Service.RemoveAllStatusEffects(Target, true);
        Service.GetCreatureStatusEffects(Target).GetAllEffects().Should().BeEmpty();
    }

    [Test]
    public void RemoveFirst_ByCleanseCategoryTypeAndBeneficialTakeOneAndReportIt()
    {
        Applied<ProbeEffect>(Target, Caster, probe =>
        {
            probe.CleanseValue = StatusEffectCleanseType.Purify;
            probe.CategoriesValue = StatusEffectCategory.Debuff;
        });
        Service.RemoveFirstCleanseableStatusEffect(Target, StatusEffectCleanseType.TreatmentKit1).Should().BeFalse();
        Service.RemoveFirstCleanseableStatusEffect(Target, StatusEffectCleanseType.Purify).Should().BeTrue();
        Service.GetCreatureStatusEffects(Target).GetAllEffects().Should().BeEmpty();

        Applied<ProbeEffect>(Target, Caster, probe => probe.CategoriesValue = StatusEffectCategory.Debuff);
        Service.RemoveFirstStatusEffectByCategory(Target, StatusEffectCategory.Buff).Should().BeFalse();
        Service.RemoveFirstStatusEffectByCategory(Target, StatusEffectCategory.Debuff).Should().BeTrue();

        Applied<ProbeEffect>(Target, Caster);
        Service.RemoveFirstStatusEffect(Target, new[] { typeof(OtherProbeEffect) }).Should().BeFalse();
        Service.RemoveFirstStatusEffect(Target, null!).Should().BeFalse();
        Service.RemoveFirstStatusEffect(Target, new Type[] { null! }).Should().BeFalse();
        Service.RemoveFirstStatusEffect(Target, new[] { typeof(ProbeEffect), null! }).Should().BeTrue();

        Applied<ProbeEffect>(Target, Caster, probe => probe.CategoriesValue = StatusEffectCategory.Debuff);
        Service.RemoveFirstBeneficialCombatStatusEffect(Target).Should().BeFalse();
        Service.RemoveStatusEffect(typeof(ProbeEffect), Target);
        Applied<ProbeEffect>(Target, Caster, probe => probe.CategoriesValue = StatusEffectCategory.Buff);
        Service.RemoveFirstBeneficialCombatStatusEffect(Target).Should().BeTrue();
    }

    [Test]
    public void RemoveCleanseable_TakesEveryMatchButNotCraftingStyles()
    {
        Applied<ProbeEffect>(Target, Caster, probe => probe.CleanseValue = StatusEffectCleanseType.Purify);
        Applied<OtherProbeEffect>(Target, Caster, probe => probe.CleanseValue = StatusEffectCleanseType.Purify);
        var keep = Applied<ThirdProbeEffect>(Target, Caster, probe =>
        {
            probe.CleanseValue = StatusEffectCleanseType.Purify;
            probe.SourceValue = StatusEffectSourceType.CraftingStyle;
        });

        Service.RemoveCleanseableStatusEffects(Target, StatusEffectCleanseType.Purify, false);

        Service.GetCreatureStatusEffects(Target).GetAllEffects().Should().Equal(keep);
    }

    [Test]
    public void RemoveWithStat_TakesEveryContributorAndNegativeOnlyTakesPenalties()
    {
        Applied<ProbeEffect>(Target, Caster, probe => probe.StatGroup.Stats[StatType.Attack] = 3);
        var penalty = Applied<OtherProbeEffect>(Target, Caster, probe => probe.StatGroup.Stats[StatType.Attack] = -3);
        var bonus = Applied<ThirdProbeEffect>(Target, Caster, probe => probe.StatGroup.Stats[StatType.Defense] = 3);

        Service.RemoveStatusEffectsWithNegativeStat(Target, StatType.Attack);
        Service.GetCreatureStatusEffects(Target).GetAllEffects().Should().NotContain(penalty);

        Service.RemoveStatusEffectsWithStat(Target, StatType.Attack, false);
        Service.GetCreatureStatusEffects(Target).GetAllEffects().Should().Equal(bonus);
    }

    [Test]
    public void RemoveFromAllTargetsBySourceClass_ReachesEveryTargetOfThatSource()
    {
        Applied<ProbeEffect>(Target, Caster);
        Applied<ProbeEffect>(Bystander, Caster);
        var other = Applied<ProbeEffect>(Caster, Bystander);

        Service.RemoveStatusEffectFromAllTargetsBySource(typeof(ProbeEffect), Caster);

        Service.HasEffect(typeof(ProbeEffect), Target).Should().BeFalse();
        Service.HasEffect(typeof(ProbeEffect), Bystander).Should().BeFalse();
        Service.GetCreatureStatusEffects(Caster).GetAllEffects().Should().Equal(other);
    }

    [Test]
    public void RemoveFromAllTargetsBySourceClass_NeedsATypeAndAValidSource()
    {
        var effect = Applied<ProbeEffect>(Target, Caster);

        Service.RemoveStatusEffectFromAllTargetsBySource(null!, Caster);
        Bench.Invalid.Add(Caster);
        Service.RemoveStatusEffectFromAllTargetsBySource(typeof(ProbeEffect), Caster);

        Service.GetCreatureStatusEffects(Target).GetAllEffects().Should().Equal(effect);
    }

    [Test]
    public void RemoveOwned_ReachesActiveAndLoggedOutTargetsAndHonoursTheAbility()
    {
        const uint Player = 700;
        const uint Returned = 701;
        var ability = new AbilityDetail { Name = "Mine" };
        Bench.Players.Add(Player);
        Bench.Players.Add(Returned);
        Bench.Engine.SetReturnHandler("GetObjectUUID", _ => "player-uuid");

        var ownedHeld = Applied<ProbeEffect>(Player, Caster, probe => probe.OriginatingAbility = ability);
        var otherAbilityHeld = Applied<OtherProbeEffect>(Player, Caster, probe => probe.OriginatingAbility = new AbilityDetail());
        Bench.Engine.SetReturn("GetExitingObject", Player);
        Service.OnPlayerExit();

        var ownedActive = Applied<ProbeEffect>(Target, Caster, probe => probe.OriginatingAbility = ability);
        var unknownOrigin = Applied<OtherProbeEffect>(Target, Caster);
        Log.Clear();

        Service.RemoveStatusEffectsFromAllTargetsBySource(Caster, typeof(ProbeEffect), false, ability);

        Service.GetCreatureStatusEffects(Target).GetAllEffects().Should().Equal(unknownOrigin);
        Log.Should().Contain($"Remove:{ownedActive.Name}:{Target}:False");
        Log.Should().NotContain(line => line.Contains("worn off"));

        Bench.Engine.SetReturn("GetEnteringObject", Returned);
        Service.OnPlayerEnter();
        var restored = Service.GetCreatureStatusEffects(Returned).GetAllEffects();
        restored.Should().Equal(otherAbilityHeld);
        restored.Should().NotContain(ownedHeld);
    }

    [Test]
    public void RemoveOwned_WithoutAnAbilityTakesTheWholeSourceAndNeedsAValidOne()
    {
        Applied<ProbeEffect>(Target, Caster);
        Service.RemoveStatusEffectsFromAllTargetsBySource(Invalid, typeof(ProbeEffect));
        Service.RemoveStatusEffectsFromAllTargetsBySource(Caster, null!);
        Service.HasEffect(typeof(ProbeEffect), Target).Should().BeTrue();

        Service.RemoveStatusEffectsFromAllTargetsBySource(Caster, typeof(StatusEffectBase));
        Service.HasEffect(typeof(ProbeEffect), Target).Should().BeFalse("derived types count");
    }

    [Test]
    public void GetSourceOwned_MatchesDerivedTypesAndNeverTreatsAnUnknownOriginAsAWildcard()
    {
        var ability = new AbilityDetail();
        var mine = Probe<OtherProbeEffect>(probe => probe.OriginatingAbility = ability);
        mine.ApplyEffect(Caster, Target, 5);
        var unknown = Probe<ProbeEffect>();
        unknown.ApplyEffect(Caster, Target, 5);
        var others = Probe<ProbeEffect>(probe => probe.OriginatingAbility = ability);
        others.ApplyEffect(Bystander, Target, 5);

        Service.GetSourceOwnedStatusEffects(new IStatusEffect[] { mine, unknown, others }, typeof(ProbeEffect), Caster)
            .Should().BeEquivalentTo(new IStatusEffect[] { mine, unknown });
        Service.GetSourceOwnedStatusEffects(new IStatusEffect[] { mine, unknown, others }, typeof(ProbeEffect), Caster, ability)
            .Should().Equal(mine);
    }

    [Test]
    public void ANewStance_RemovesOtherStancesAndTellsTheMeleeTechnique()
    {
        Service.DefinitionTypes = new[] { typeof(StanceProbeEffect), typeof(ProbeEffect) };
        Service.CacheData();
        var old = Applied<ProbeEffect>(Target, Caster, probe => probe.SourceValue = StatusEffectSourceType.Stance);

        Service.RemoveOtherStanceStatuses(Target, typeof(StanceProbeEffect));

        Log.Should().ContainInOrder($"OnStanceChanged:{Target}", $"Remove:{old.Name}:{Target}:False");
        Service.GetCreatureStatusEffects(Target).GetAllEffects().Should().BeEmpty();
    }

    [Test]
    public void TheStanceRuleKeepsTheNewTypeAndIgnoresOtherEffects()
    {
        Service.DefinitionTypes = new[] { typeof(StanceProbeEffect), typeof(ProbeEffect) };
        Service.CacheData();
        var same = Applied<StanceProbeEffect>(Target, Caster);

        Service.RemoveOtherStanceStatuses(Target, typeof(ProbeEffect));
        Log.Should().BeEmpty("the type is not a stance");

        Service.RemoveOtherStanceStatuses(Target, typeof(StanceProbeEffect));
        Service.GetCreatureStatusEffects(Target).GetAllEffects().Should().Equal(same);
    }

    public class StanceProbeEffect : ProbeEffect
    {
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Stance;
    }

    public class CommandProbeEffect : ProbeEffect
    {
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Command;
    }

    public class OtherCommandProbeEffect : CommandProbeEffect
    {
    }

    [Test]
    public void ANewCommand_RemovesOnlyTheSameLeadersOtherCommandsAsAReplacement()
    {
        Service.DefinitionTypes = new[] { typeof(CommandProbeEffect), typeof(OtherCommandProbeEffect), typeof(ProbeEffect) };
        Service.CacheData();
        var mine = Applied<OtherCommandProbeEffect>(Target, Caster);
        var theirs = Applied<ThirdProbeEffect>(Target, Bystander, probe => probe.SourceValue = StatusEffectSourceType.Command);

        Service.RemoveOtherCommandStatuses(Target, typeof(ProbeEffect), Caster);
        Service.GetCreatureStatusEffects(Target).GetAllEffects().Should().HaveCount(2, "the type is not a command");

        Service.RemoveOtherCommandStatuses(Target, typeof(CommandProbeEffect), Caster);

        Log.Should().Contain($"Remove:{mine.Name}:{Target}:True");
        Log.Should().NotContain(line => line.StartsWith("Refresh:"), "a replacement does not refresh the sheet");
        Service.GetCreatureStatusEffects(Target).GetAllEffects().Should().Equal(theirs);
    }

    [Test]
    public void ApplyingACommand_RemovesTheLeadersOtherCommandFirst()
    {
        Service.DefinitionTypes = new[] { typeof(CommandProbeEffect), typeof(OtherCommandProbeEffect) };
        Service.CacheData();
        var old = Applied<OtherCommandProbeEffect>(Target, Caster);

        Service.ApplyStatusEffect(Caster, Target, typeof(CommandProbeEffect), 5f).Should().BeTrue();

        Service.GetStatusEffect(Target, typeof(OtherCommandProbeEffect)).Should().BeNull();
        Log.Should().Contain($"Remove:{old.Name}:{Target}:True");
    }

    [Test]
    public void RemovingBySourceType_HonoursTheExclusionAndTheSourceFilter()
    {
        var excluded = Applied<ProbeEffect>(Target, Caster, probe => probe.SourceValue = StatusEffectSourceType.Stance);
        var filtered = Applied<OtherProbeEffect>(Target, Bystander, probe => probe.SourceValue = StatusEffectSourceType.Stance);
        Applied<ThirdProbeEffect>(Target, Caster, probe => probe.SourceValue = StatusEffectSourceType.Stance);

        Service.RemoveStatusEffectBySourceType(Target, StatusEffectSourceType.Stance, false, typeof(ProbeEffect), true, Caster);

        Service.GetCreatureStatusEffects(Target).GetAllEffects().Should().BeEquivalentTo(new IStatusEffect[] { excluded, filtered });

        Service.RemoveStatusEffectBySourceType(Bystander, StatusEffectSourceType.Stance);
        Service.GetCreatureStatusEffects(Target).GetAllEffects().Should().HaveCount(2);
    }
}
