using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>Weapon classes can require valid equipment without coupling shared attacks to their implementation.</summary>
    public interface IWeaponAttackEligibilityService
    {
        void RegisterRule(BaseItem type, Func<uint, uint, WeaponAttackPhase, bool> rule);

        bool IsAllowed(uint creature, uint weapon, WeaponAttackPhase phase);

        bool IsAllowed(BaseItem type, uint creature, uint weapon, WeaponAttackPhase phase);
    }
}
