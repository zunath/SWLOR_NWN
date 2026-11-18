#nullable enable
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWScript;

namespace SWLOR.Game.Server.Tests.Support;

internal sealed partial class ReviveProbe : DeathService
{
    public ReviveProbe(IEnumerable<ICreatureRevivedListener> listeners)
        : base(null!, null!, null!, null!, null!, null!, null!, null!, null!, listeners, null!)
    {
    }

    public void Revive(uint creature) => ReapplyAuraEffectsForCreature(creature);
}
