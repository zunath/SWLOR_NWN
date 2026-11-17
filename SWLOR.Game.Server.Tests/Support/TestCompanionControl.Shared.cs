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

// Seams that more than one part calls: the clock (CC-1 and CC-4), the busy check (CC-1 and CC-3) and the ability interruption (CC-1 and CC-3).
public sealed partial class TestCompanionControl
{
    protected override DateTime UtcNow() => Now;

    protected override bool IsBusy(uint target) => Busy.Contains(target);

    protected override bool InterruptAbilityActivation(uint activator)
    {
        InterruptedActivations.Add(activator);
        return false;
    }
}
