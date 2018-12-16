using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface ICombatService
    {
        void OnModuleApplyDamage();
        int CalculateForceAccuracy(NWCreature caster, NWCreature target, ForceAbilityType abilityType, CustomAttribute primaryAttribute);
    }
}