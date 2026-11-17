#nullable enable
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using static SWLOR.NWN.API.NWScript.NWScript;

namespace SWLOR.Game.Server.Tests.Support;

// Part CC-1, CompanionControlService (stays): the rest release, the toggle abilities and the display name.
public sealed partial class TestCompanionControl
{
    protected override ActivityStatusType GetBusyType(uint target) =>
        BusyTypes.GetValueOrDefault(target, ActivityStatusType.Invalid);

    protected override void ClearBusy(uint target) => ClearedBusy.Add(target);

    protected override bool HasRestStatusEffect(uint creature) => RestEffect.Contains(creature);

    protected override void RemoveRestStatusEffect(uint creature) => RestEffect.Remove(creature);

    protected override void DequeueWeaponAbility(uint creature) => Dequeued.Add(creature);

    protected override string GetDisplayName(uint observer, uint target) => GetName(target);
}
