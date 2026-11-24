using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The damage a guard returns on a guarded hit: the retaliation and its pulse.
    /// </summary>
    public interface ICombatGuardRetaliationService
    {
        void ApplyGuardedHitRetaliation(uint attacker, uint defender, int incomingDamage);
    }
}
