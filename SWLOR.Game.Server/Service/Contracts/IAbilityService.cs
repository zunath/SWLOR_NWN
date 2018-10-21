using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Perk;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IAbilityService
    {
        void OnHitCastSpell(NWPlayer oPC);
        void OnModuleUseFeat();
        void RestoreFP(NWPlayer oPC, int amount);
        PlayerCharacter RestoreFP(NWPlayer oPC, int amount, PlayerCharacter entity);
        void OnModuleApplyDamage();
        void ApplyCooldown(NWPlayer pc, CooldownCategory cooldown, IPerk ability);
        void ApplyEnmity(NWPlayer pc, NWCreature target, Data.Perk perk);
        void HandleQueueWeaponSkill(NWPlayer pc, Data.Perk entity, IPerk ability);
    }
}
