using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class OrdinaryAttackHasteStateTests
{
    private static readonly DateTime Start = new(2026, 10, 13, 12, 0, 0, DateTimeKind.Utc);
    private static readonly OrdinaryAttackHasteConfiguration Configuration = new(2, 5, 2, 6);

    [Test]
    public void SameTargetHits_ReachFiveAndRefreshAtCap_WithoutAcceleratingGrantCooldown()
    {
        var state = new OrdinaryAttackHasteState();
        for (var index = 0; index < 5; index++)
        {
            state.RecordHit(Start.AddSeconds(index * 2), Configuration, 10, index + 1).Should().BeTrue();
            state.Stacks.Should().Be(index + 1);
        }
        state.RecordHit(Start.AddSeconds(9), Configuration, 10, 6).Should().BeTrue();
        state.Stacks.Should().Be(5);
        state.Expires.Should().Be(Start.AddSeconds(15));
        state.RecordTargetChange(20);
        state.Stacks.Should().Be(0);
        state.RecordHit(Start.AddSeconds(9.5), Configuration, 20, 7);
        state.Stacks.Should().Be(0, "target changes preserve the previous grant cooldown");
        state.RecordHit(Start.AddSeconds(10), Configuration, 20, 8);
        state.Stacks.Should().Be(1);
        state.Reconcile(Start.AddSeconds(16), Configuration);
        state.Stacks.Should().Be(0);
    }

    [Test]
    public void DuplicateHits_CannotGrantOrRefresh_AndMissingHitsExpire()
    {
        var state = new OrdinaryAttackHasteState();
        state.RecordHit(Start, Configuration, 10, 1);
        state.RecordHit(Start.AddSeconds(4), Configuration, 10, 1).Should().BeFalse();
        state.Stacks.Should().Be(1);
        state.Expires.Should().Be(Start.AddSeconds(6));
        state.Reconcile(Start.AddSeconds(6), Configuration);
        state.Stacks.Should().Be(0);
    }

    [Test]
    public void ConfiguredStackLimit_DrivesStateAndRenderedHaste()
    {
        var state = new OrdinaryAttackHasteState();
        var configuration = Configuration with { MaximumStacks = 6 };
        for (var index = 0; index < 6; index++)
            state.RecordHit(Start.AddSeconds(index * 2), configuration, 10, index + 1);
        state.Stacks.Should().Be(6);
        var status = new OrdinaryAttackHasteStatusEffect(state.Stacks, configuration.HastePerStack);
        status.StatGroup.Stats[StatType.AttackDelayReductionPercent].Should().Be(12);
        status.Icon.Should().Be(EffectIconType.OrdinaryAttackHasteStatusEffect5);
    }

    [Test]
    public void CombatOrStanceReset_KeepsCooldown_WhileOrdinaryReconcilePreservesStacks()
    {
        var state = new OrdinaryAttackHasteState();
        state.RecordHit(Start, Configuration, 10, 1);
        state.Reconcile(Start.AddSeconds(1), Configuration);
        state.Stacks.Should().Be(1, "weapon changes do not reset the independent ordinary-hit state");
        state.Clear();
        state.RecordHit(Start.AddSeconds(1), Configuration, 10, 2);
        state.Stacks.Should().Be(0);
        state.RecordHit(Start.AddSeconds(2), Configuration, 10, 3);
        state.Stacks.Should().Be(1);
        state.Reconcile(Start.AddSeconds(3), Configuration with { MaximumStacks = 0 });
        state.Stacks.Should().Be(0, "leaving the granting stance removes its ramp");
    }
}
