using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IAbilityService
    {
        void OnHitCastSpell(NWPlayer oPC);
        void OnModuleUseFeat();
        void RestoreFP(NWPlayer oPC, int amount);
        PlayerCharacter RestoreFP(NWPlayer oPC, int amount, PlayerCharacter entity);
        void OnModuleApplyDamage();
    }
}
