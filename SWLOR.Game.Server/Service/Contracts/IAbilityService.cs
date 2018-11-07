using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Perk;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IAbilityService
    {
        void OnHitCastSpell(NWPlayer oPC);
        void OnModuleUseFeat();
        void RestoreFP(NWPlayer oPC, int amount);
        Player RestoreFP(NWPlayer oPC, int amount, Player entity);
        void OnModuleApplyDamage();
        void ApplyCooldown(NWPlayer pc, CooldownCategory cooldown, IPerk ability);
        void ApplyEnmity(NWPlayer pc, NWCreature target, Data.Entity.Perk perk);
        void HandleQueueWeaponSkill(NWPlayer pc, Data.Entity.Perk entity, IPerk ability);
    }
}
