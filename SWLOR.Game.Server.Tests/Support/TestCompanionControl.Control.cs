#nullable enable
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Creatures;
using static SWLOR.NWN.API.NWScript.NWScript;

namespace SWLOR.Game.Server.Tests.Support;

// Part CC-1, CompanionControlService (stays): the clock, the busy check, the ability interruption, the rest release, the
// toggle abilities and the display name.
public sealed partial class TestCompanionControl
{
    protected override DateTime UtcNow() => State.Now;

    protected override bool IsBusy(uint target) => State.Busy.Contains(target);

    protected override bool InterruptAbilityActivation(uint activator)
    {
        State.InterruptedActivations.Add(activator);
        return false;
    }

    protected override ActivityStatusType GetBusyType(uint target) =>
        State.BusyTypes.GetValueOrDefault(target, ActivityStatusType.Invalid);

    protected override void ClearBusy(uint target) => State.ClearedBusy.Add(target);

    protected override bool HasRestStatusEffect(uint creature) => State.RestEffect.Contains(creature);

    protected override void RemoveRestStatusEffect(uint creature) => State.RestEffect.Remove(creature);

    protected override void DequeueWeaponAbility(uint creature) => State.Dequeued.Add(creature);

    protected override string GetDisplayName(uint observer, uint target) => GetName(target);
}
