using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface ICombatService
    {
        void OnModuleApplyDamage();
        ForceDamageResult CalculateForceDamage(
            NWCreature caster,
            NWCreature target,
            ForceAbilityType abilityType,
            int basePotency,
            float tier1Modifier,
            float tier2Modifier,
            float tier3Modifier,
            float tier4Modifier);
        float CalculateResistanceRating(NWCreature caster, NWCreature target, ForceAbilityType forceAbility);
    }
}