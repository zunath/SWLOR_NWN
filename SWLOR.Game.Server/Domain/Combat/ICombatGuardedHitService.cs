using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The guarded hit part of combat: the reduction a landed Guard roll gives a physical hit, with its stamina recovery,
    /// retaliation and enmity.
    /// </summary>
    public interface ICombatGuardedHitService
    {
        int ApplyGuardedHitModifiers(
            uint defender,
            uint attacker,
            int damage,
            CombatDamageType damageType,
            bool isLandedAttack);
    }
}
