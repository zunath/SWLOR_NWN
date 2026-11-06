using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>What a player keeps across logout, the module event handlers, and the interval tick.</summary>
[TestFixture]
public class StatusEffectServiceLifecycleTests : StatusEffectServiceTestBase
{
    private const uint Player = 500;
    private const uint ReturningPlayer = 501;

    private void Exit(uint player = Player)
    {
        Bench.Engine.SetReturn("GetExitingObject", player);
        Service.OnPlayerExit();
    }

    private void Enter(uint player)
    {
        Bench.Engine.SetReturn("GetEnteringObject", player);
        Service.OnPlayerEnter();
    }

    /// <summary>The same player id on a new object handle, as a reconnect gives.</summary>
    private void SameUuid(params uint[] handles) =>
        Bench.Engine.SetReturnHandler("GetObjectUUID", _ => "player-uuid");

    private void LastScriptEffectTag(string tag, string fallback = "")
    {
        Bench.Engine.SetReturn("GetEffectTag", tag);
        Bench.Engine.SetReturn("GetEffectString", fallback);
    }

    [SetUp]
    public void Players()
    {
        Bench.Players.Add(Player);
        Bench.Players.Add(ReturningPlayer);
    }

    [Test]
    public void Enter_IgnoresNonPlayersAndDMs()
    {
        Bench.Players.Remove(Player);
        Enter(Player);
        Log.Should().BeEmpty();

        Bench.Players.Add(Player);
        Bench.Dms.Add(Player);
        Enter(Player);
        Log.Should().BeEmpty();
    }

    [Test]
    public void Enter_WithNothingHeldStillReappliesFoodHP()
    {
        Enter(Player);

        Log.Should().Equal($"ReapplyFoodHP:{Player}");
        Service.GetCreatureStatusEffects(Player).GetAllEffects().Should().BeEmpty();
    }

    [Test]
    public void Exit_IgnoresNonPlayersAndDMs()
    {
        var effect = Applied<ProbeEffect>(Bystander, Bystander);
        Exit(Bystander);
        Service.HasEffect(typeof(ProbeEffect), Bystander).Should().BeTrue();

        Bench.Players.Add(Bystander);
        Bench.Dms.Add(Bystander);
        Exit(Bystander);
        Service.GetCreatureStatusEffects(Bystander).GetAllEffects().Should().Contain(effect);
    }

    [Test]
    public void Exit_WithNoEffectsClearsTheCreature()
    {
        Exit();

        Bench.Engine.CallsTo("RemoveEffectByTag").Should().ContainSingle();
    }

    [Test]
    public void Exit_RemovesWhatDoesNotPersistAndClearsTheCreatureWhenNothingIsLeft()
    {
        var effect = Applied<ProbeEffect>(Player, Player, probe => probe.Persists = false);

        Exit();

        Log.Should().Contain($"Remove:{effect.Name}:{Player}:False");
        Service.HasEffect(typeof(ProbeEffect), Player).Should().BeFalse();
        Bench.Engine.Calls.Where(call => call.Member == "RemoveEffectByTag")
            .Should().Contain(call => ((string[])call.Arguments[1]!).Contains("STATUS_EFFECT"));

        Log.Clear();
        Enter(Player);
        Service.HasEffect(typeof(ProbeEffect), Player).Should().BeFalse("nothing was held for the return");
    }

    [Test]
    public void ReconnectingOnANewHandle_RestoresWhatPersistsAndTakesOverItsOwnEffects()
    {
        SameUuid();
        var own = Applied<ProbeEffect>(Player, Player);
        var fromOther = Applied<OtherProbeEffect>(Player, Caster, seconds: 30f);

        Exit(Player);

        Service.GetCreatureStatusEffects(Player).GetAllEffects().Should().BeEmpty("the tracker waits for the return");
        Bench.Engine.Calls.Where(call => call.Member == "RemoveEffectByTag")
            .Should().Contain(call => ((string[])call.Arguments[1]!).Contains(own.Id));

        Log.Clear();
        Bench.Engine.ClearCalls();
        Enter(ReturningPlayer);

        Service.GetCreatureStatusEffects(ReturningPlayer).GetAllEffects().Should().BeEquivalentTo(new IStatusEffect[] { own, fromOther });
        own.Source.Should().Be(ReturningPlayer, "an effect the player gave themselves follows the new handle");
        fromOther.Source.Should().Be(Caster);
        Log.Should().Contain($"Reapply:{own.Name}:{ReturningPlayer}").And.EndWith($"ReapplyFoodHP:{ReturningPlayer}");
        Bench.Engine.CallsTo("ApplyEffectToObject").Should().HaveCount(2);
    }

    [Test]
    public void ReconnectingTwice_RestoresOnlyOnce()
    {
        SameUuid();
        Applied<ProbeEffect>(Player, Player);
        Exit(Player);
        Enter(ReturningPlayer);
        Service.RemoveCreature(ReturningPlayer);
        Log.Clear();

        Enter(ReturningPlayer);

        Service.HasEffect(typeof(ProbeEffect), ReturningPlayer).Should().BeFalse();
        Log.Should().Equal($"ReapplyFoodHP:{ReturningPlayer}");
    }

    [Test]
    public void ReconnectingAfterTheDuration_RemovesTheEffectAndTellsItsHandler()
    {
        SameUuid();
        var expired = Applied<HandlerProbeEffect>(Player, Player, seconds: 10f);
        var kept = Applied<ProbeEffect>(Player, Player, seconds: 60f);
        Exit(Player);
        Log.Clear();
        Now = Start.AddSeconds(30);

        Enter(ReturningPlayer);

        expired.IsFlaggedForRemoval.Should().BeTrue();
        Log.Should().Contain($"Remove:{expired.Name}:{ReturningPlayer}:False");
        Log.Should().Contain($"AfterRemoved:{expired.Name}:{ReturningPlayer}");
        Service.GetCreatureStatusEffects(ReturningPlayer).GetAllEffects().Should().Equal(kept);
        kept.DurationTicks.Should().Be(30);
    }

    [Test]
    public void ReconnectingAfterEverythingExpired_LeavesNoTracker()
    {
        SameUuid();
        Applied<ProbeEffect>(Player, Player, seconds: 10f);
        Exit(Player);
        Log.Clear();
        Now = Start.AddSeconds(30);

        Enter(ReturningPlayer);

        Service.GetCreatureStatusEffects(ReturningPlayer).GetAllEffects().Should().BeEmpty();
        Log.Should().EndWith($"ReapplyFoodHP:{ReturningPlayer}");
        Log.Should().NotContain(line => line.StartsWith("Reapply:"));
    }

    [Test]
    public void AClockThatMovedBackwards_CountsNoTimeAsPassed()
    {
        SameUuid();
        var effect = Applied<ProbeEffect>(Player, Player, seconds: 10f);
        Exit(Player);
        Now = Start.AddHours(-1);

        Enter(ReturningPlayer);

        effect.IsFlaggedForRemoval.Should().BeFalse();
        effect.DurationTicks.Should().Be(10);
    }

    [Test]
    public void RestoringAnEffectThatChangesStats_ReappliesTheMovementRateOnceAndTellsItsHandler()
    {
        SameUuid();
        var first = Applied<HandlerProbeEffect>(Player, Player);
        first.StatGroup.Stats[StatType.Attack] = 5;
        var second = Applied<ProbeEffect>(Player, Player);
        second.StatGroup.Stats[StatType.Attack] = 5;
        Exit(Player);
        Log.Clear();

        Enter(ReturningPlayer);

        Log.Count(line => line == $"ApplyCreatureMovementRate:{ReturningPlayer}").Should().Be(1);
        Log.Should().Contain($"AfterRestored:{first.Name}:{ReturningPlayer}");
    }

    [Test]
    public void RestoringAnEffectWhoseNativeEffectStillExists_DoesNotApplyItAgain()
    {
        SameUuid();
        var effect = Applied<ProbeEffect>(Player, Player);
        Exit(Player);
        Bench.Engine.ClearCalls();
        Bench.NativeTags.Add(effect.Id);

        Enter(ReturningPlayer);

        Bench.Engine.CallsTo("ApplyEffectToObject").Should().BeEmpty();
    }

    [Test]
    public void SourceExit_TakesItsMarkedEffectsOffEveryTargetAndOutOfEveryLoggedOutPlayer()
    {
        SameUuid();
        var marked = Applied<RemoveOnExitProbeEffect>(Target, Player);
        var unmarked = Applied<ProbeEffect>(Target, Player);
        var heldEffect = Applied<RemoveOnExitProbeEffect>(ReturningPlayer, Player);
        var heldOther = Applied<OtherProbeEffect>(ReturningPlayer, Caster);

        Bench.Engine.SetReturnHandler("GetObjectUUID", args => $"uuid-{args[0]}");
        Exit(ReturningPlayer);
        Service.GetCreatureStatusEffects(ReturningPlayer).GetAllEffects().Should().BeEmpty();
        Log.Clear();

        Exit(Player);

        Service.GetCreatureStatusEffects(Target).GetAllEffects().Should().Equal(unmarked);
        Log.Should().Contain($"Remove:{marked.Name}:{Target}:False");

        Enter(ReturningPlayer);
        Service.GetCreatureStatusEffects(ReturningPlayer).GetAllEffects().Should().Equal(heldOther);
        heldEffect.IsFlaggedForRemoval.Should().BeFalse("it is dropped from the held list, not run");
    }

    [Test]
    public void SourceExit_OfAnInvalidObjectTouchesNoOtherTarget()
    {
        Bench.Invalid.Add(Player);
        var marked = Applied<RemoveOnExitProbeEffect>(Target, Player);

        Exit(Player);

        Service.GetCreatureStatusEffects(Target).GetAllEffects().Should().Equal(marked);
    }

    [Test]
    public void ClearOnDeath_SkipsNonPlayersAndDMs()
    {
        var effect = Applied<ProbeEffect>(Bystander, Bystander);

        Service.ClearStatusEffectsOnDeath(Bystander);
        Service.GetCreatureStatusEffects(Bystander).GetAllEffects().Should().Equal(effect);

        Bench.Players.Add(Bystander);
        Bench.Dms.Add(Bystander);
        Service.ClearStatusEffectsOnDeath(Bystander);
        Service.GetCreatureStatusEffects(Bystander).GetAllEffects().Should().Equal(effect);
    }

    [Test]
    public void ClearOnDeath_RemovesTheEffectsAndKeepsAnyCraftingStyle()
    {
        Applied<ProbeEffect>(Player, Player);
        var style = Applied<OtherProbeEffect>(Player, Player, probe => probe.SourceValue = StatusEffectSourceType.CraftingStyle);

        Service.ClearStatusEffectsOnDeath(Player);

        Service.GetCreatureStatusEffects(Player).GetAllEffects().Should().Equal(style);
        Bench.Engine.CallsTo("RemoveEffectByTag")
            .Should().NotContain(call => ((string[])call.Arguments[1]!).Contains("STATUS_EFFECT"));
    }

    [Test]
    public void ClearOnDeath_ClearsTheCreatureWhenNothingIsLeft()
    {
        Service.ClearStatusEffectsOnDeath(Player);
        Bench.Engine.CallsTo("RemoveEffectByTag")
            .Should().ContainSingle(call => ((string[])call.Arguments[1]!).Contains("STATUS_EFFECT"));

        Bench.Engine.ClearCalls();
        Applied<ProbeEffect>(Player, Player);
        Service.ClearStatusEffectsOnDeath(Player);
        Bench.Engine.CallsTo("RemoveEffectByTag")
            .Should().Contain(call => ((string[])call.Arguments[1]!).Contains("STATUS_EFFECT"));
    }

    [Test]
    public void OnApplyHandler_StartsATrackerForTheScriptObjectOnce()
    {
        Bench.Engine.SetReturn("OBJECT_SELF", Target);

        Service.OnApplyStatusEffect();
        var tracker = Service.GetCreatureStatusEffects(Target);
        Service.OnApplyStatusEffect();

        Service.GetCreatureStatusEffects(Target).Should().BeSameAs(tracker);
    }

    [TestCase("")]
    [TestCase("STATUS_EFFECT")]
    public void OnRemoveHandler_OfTheOldSharedEffectForgetsTheCreature(string tag)
    {
        Applied<ProbeEffect>(Target, Caster);
        Bench.Engine.SetReturn("OBJECT_SELF", Target);
        LastScriptEffectTag(tag);

        Service.OnRemoveStatusEffect();

        Service.HasEffect(typeof(ProbeEffect), Target).Should().BeFalse();
        Bench.Engine.CallsTo("DelayCommand").Should().BeEmpty();
    }

    [Test]
    public void OnRemoveHandler_OfAnUntrackedCreatureIsHarmless()
    {
        Bench.Engine.SetReturn("OBJECT_SELF", Target);
        LastScriptEffectTag(string.Empty);

        Service.OnRemoveStatusEffect();

        Service.HasEffect(typeof(ProbeEffect), Target).Should().BeFalse();
    }

    [Test]
    public void OnRemove_AfterNativeRemovalSettles_ClearsAStatusWithoutATimerAndKeepsTheRenewedOne()
    {
        var effect = Applied<ProbeEffect>(Target, Caster);
        LastScriptEffectTag(effect.Id);

        Service.OnRemoveNWNStatusEffect(Target);
        Service.HasEffect(typeof(ProbeEffect), Target).Should().BeTrue("the check waits for native removal to settle");

        Bench.NativeTags.Add(effect.Id);
        Bench.RunDeferred("DelayCommand");
        Service.HasEffect(typeof(ProbeEffect), Target).Should().BeTrue("a renewed timer carries the same tag");

        Bench.NativeTags.Remove(effect.Id);
        Bench.RunDeferred("DelayCommand");
        Service.HasEffect(typeof(ProbeEffect), Target).Should().BeFalse();
        Bench.Engine.CallsTo("DelayCommand").Single().Arguments[0].Should().Be(0f);
    }

    [Test]
    public void OnRemove_UsesTheEffectStringWhenTheTagIsEmpty()
    {
        var effect = Applied<ProbeEffect>(Target, Caster);
        LastScriptEffectTag(string.Empty, effect.Id);

        Service.OnRemoveNWNStatusEffect(Target);
        Bench.RunDeferred("DelayCommand");

        Service.HasEffect(typeof(ProbeEffect), Target).Should().BeFalse();
    }

    [Test]
    public void OnRemove_DuringARemovalTheServiceMadeItselfIsIgnored()
    {
        var effect = Applied<ProbeEffect>(Target, Caster);
        LastScriptEffectTag(effect.Id);
        Bench.Engine.OnRemoveEffectByTag = (creature, tags) =>
        {
            if (tags.Contains(effect.Id))
                Service.OnRemoveNWNStatusEffect(creature);
        };

        Service.RemoveStatusEffect(Target, typeof(ProbeEffect));

        Bench.Engine.CallsTo("DelayCommand").Where(call => (float)call.Arguments[0]! == 0f)
            .Should().BeEmpty("the service caused this removal and has already handled it");

        Bench.Engine.OnRemoveEffectByTag = null;
        Service.OnRemoveNWNStatusEffect(Target);
        Bench.Engine.CallsTo("DelayCommand").Where(call => (float)call.Arguments[0]! == 0f)
            .Should().ContainSingle("the suppression ends with the removal");
    }

    [TestCase("")]
    [TestCase("STATUS_EFFECT")]
    public void TheSharedInterval_TicksEveryTickingEffect(string tag)
    {
        var ticking = Applied<ProbeEffect>(Target, Caster);
        var passive = Applied<OtherProbeEffect>(Target, Caster, probe => probe.ActivationValue = StatusEffectActivationType.Passive);
        Bench.Engine.SetReturn("OBJECT_SELF", Target);
        LastScriptEffectTag(tag);
        Now = Start.AddSeconds(1);

        Service.OnStatusEffectInterval();

        Log.Should().Equal($"Tick:{ticking.Name}:{Target}");
        passive.IsFlaggedForRemoval.Should().BeFalse();
    }

    [Test]
    public void TheSharedInterval_ClearsADeadOrInvalidCreature()
    {
        Applied<ProbeEffect>(Target, Caster);
        Bench.Dead.Add(Target);
        LastScriptEffectTag(string.Empty);

        Service.OnNWNStatusEffectInterval(Target);

        Service.HasEffect(typeof(ProbeEffect), Target).Should().BeFalse();
        Bench.Engine.CallsTo("RemoveEffectByTag").Should().ContainSingle();

        Bench.Dead.Clear();
        Bench.Invalid.Add(Bystander);
        Bench.Engine.ClearCalls();
        Service.OnNWNStatusEffectInterval(Bystander);
        Bench.Engine.CallsTo("RemoveEffectByTag").Should().ContainSingle();
    }

    [Test]
    public void TheSharedInterval_OfAnUntrackedCreatureRemovesTheSharedEffect()
    {
        LastScriptEffectTag(string.Empty);

        Service.OnNWNStatusEffectInterval(Target);

        Bench.Engine.CallsTo("RemoveEffectByTag")
            .Should().ContainSingle(call => ((string[])call.Arguments[1]!).Contains("STATUS_EFFECT"));
    }

    [Test]
    public void TheEffectInterval_TicksThatEffectOnly()
    {
        var first = Applied<ProbeEffect>(Target, Caster);
        Applied<OtherProbeEffect>(Target, Caster, probe => probe.NameValue = "Other");
        LastScriptEffectTag(first.Id);
        Now = Start.AddSeconds(1);

        Service.OnNWNStatusEffectInterval(Target);

        Log.Should().Equal($"Tick:{first.Name}:{Target}");
    }

    [Test]
    public void TheEffectInterval_OnADeadCreatureRemovesTheEffectWithoutAMessage()
    {
        var effect = Applied<ProbeEffect>(Target, Caster);
        Bench.Dead.Add(Target);
        LastScriptEffectTag(effect.Id);

        Service.OnNWNStatusEffectInterval(Target);

        Service.HasEffect(typeof(ProbeEffect), Target).Should().BeFalse();
        Log.Should().NotContain(line => line.StartsWith("Nearby:"));
    }

    [Test]
    public void TheEffectInterval_RemovesANativeEffectNothingTracks()
    {
        Bench.Engine.SetReturn("HasEffectByTag", true);
        LastScriptEffectTag("orphan");

        Service.OnNWNStatusEffectInterval(Target);
        Bench.Engine.CallsTo("RemoveEffectByTag")
            .Should().ContainSingle(call => ((string[])call.Arguments[1]!).Contains("orphan"));

        Bench.Engine.ClearCalls();
        Applied<ProbeEffect>(Target, Caster);
        Service.OnNWNStatusEffectInterval(Target);
        Bench.Engine.CallsTo("RemoveEffectByTag")
            .Should().ContainSingle(call => ((string[])call.Arguments[1]!).Contains("orphan"));
    }

    [Test]
    public void TheEffectInterval_OnADeadCreatureClearsANativeEffectNothingTracks()
    {
        Bench.Dead.Add(Target);
        LastScriptEffectTag("orphan");

        Service.OnNWNStatusEffectInterval(Target);
        Bench.Engine.CallsTo("RemoveEffectByTag")
            .Should().ContainSingle(call => ((string[])call.Arguments[1]!).Contains("orphan"), "the creature has no tracker");

        Bench.Engine.ClearCalls();
        Applied<ProbeEffect>(Target, Caster);
        Service.OnNWNStatusEffectInterval(Target);
        Bench.Engine.CallsTo("RemoveEffectByTag")
            .Should().ContainSingle(call => ((string[])call.Arguments[1]!).Contains("orphan"), "the tracker does not know the tag");
    }

    [Test]
    public void AnEffectThatStoppedTicking_IsLeftAloneOnItsInterval()
    {
        var effect = Applied<ProbeEffect>(Target, Caster);
        effect.ActivationValue = StatusEffectActivationType.Passive;
        LastScriptEffectTag(effect.Id);
        Now = Start.AddSeconds(1);

        Service.OnNWNStatusEffectInterval(Target);

        Log.Should().BeEmpty();
        Service.HasEffect(typeof(ProbeEffect), Target).Should().BeTrue();
    }

    [Test]
    public void OnRemove_OfAStatusNothingTracks_NeedsNoNativeRemoval()
    {
        LastScriptEffectTag("orphan");

        Service.OnRemoveNWNStatusEffect(Target);
        Bench.RunDeferred("DelayCommand");
        Bench.Engine.CallsTo("RemoveEffectByTag").Should().BeEmpty();

        Applied<ProbeEffect>(Target, Caster);
        Bench.Engine.ClearCalls();
        Service.OnRemoveNWNStatusEffect(Target);
        Bench.RunDeferred("DelayCommand");
        Bench.Engine.CallsTo("RemoveEffectByTag").Should().BeEmpty();
        Service.HasEffect(typeof(ProbeEffect), Target).Should().BeTrue();
    }

    [Test]
    public void AnEffectFlaggedBeforeItsTick_IsRemovedWithoutTicking()
    {
        var effect = Applied<ProbeEffect>(Target, Caster);
        effect.Flag();
        LastScriptEffectTag(effect.Id);
        Now = Start.AddSeconds(1);

        Service.OnNWNStatusEffectInterval(Target);

        Log.Should().NotContain(line => line.StartsWith("Tick:"));
        Service.HasEffect(typeof(ProbeEffect), Target).Should().BeFalse();
    }

    [Test]
    public void AnEffectThatFlagsItselfDuringItsTick_IsRemovedWithAMessage()
    {
        var effect = Applied<ProbeEffect>(Target, Caster, probe => probe.FlagOnTick = true);
        LastScriptEffectTag(effect.Id);
        Now = Start.AddSeconds(1);

        Service.OnNWNStatusEffectInterval(Target);

        Log.Should().ContainInOrder($"Tick:{effect.Name}:{Target}", $"Nearby:{Target}:name-{Target}'s {effect.Name} effect has worn off.");
        Service.HasEffect(typeof(ProbeEffect), Target).Should().BeFalse();
    }

    [Test]
    public void AnEffectThatIsNotTicking_DoesNotTickOnItsOwnInterval()
    {
        var effect = Applied<ProbeEffect>(Target, Caster, probe => probe.ActivationValue = StatusEffectActivationType.Passive);
        LastScriptEffectTag(effect.Id);
        Now = Start.AddSeconds(1);

        Service.OnNWNStatusEffectInterval(Target);

        Bench.Engine.CallsTo("RemoveEffectByTag")
            .Should().ContainSingle(call => ((string[])call.Arguments[1]!).Contains(effect.Id), "it is not on the tick list");
        Log.Should().BeEmpty();
    }

    [Test]
    public void RemoveCreature_CanLeaveTheNativeEffectInPlace()
    {
        Applied<ProbeEffect>(Target, Caster);

        Service.RemoveCreature(Target, false);

        Service.HasEffect(typeof(ProbeEffect), Target).Should().BeFalse();
        Bench.Engine.CallsTo("RemoveEffectByTag").Should().BeEmpty();
    }
}
