using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The steps a landed direct hit grants the attacker, and the tracking of a hit that deals damage.
    /// </summary>
    public interface ICombatDealtEffectsService
    {
        /// <summary>
        /// Records the damage dealt for the capstone proof and the paired ranged traits, and tracks the attacker's combat
        /// activity and recent damage target. Runs for a direct hit and for triggered damage.
        /// </summary>
        void ApplyDamageDealtTracking(
            uint attacker,
            uint defender,
            int damage,
            SkillType skillType,
            CombatDamageType damageType,
            CombatDamageDeliveryType deliveryType,
            bool isAbilityDamage);

        void ApplyDamageDealtHPPercentRestore(uint attacker, int damage);

        void ApplyPhysicalDamageDealtHPPercentRestore(uint attacker, int damage, CombatDamageType damageType);

        void ApplyLowHPDamageDealtHPRestore(uint attacker, int damage);

        void ApplyHeavyVibrobladeDefenseDamageRecovery(uint attacker, int damage);

        void ApplyPredatorsMarkEffects(uint attacker, uint defender, bool isAbilityDamage);

        void ApplyBleedingTargetStaminaRestore(uint attacker, uint defender, SkillType skillType, bool isAbilityDamage);

        void ApplyDamageDealtForceErosionEffect(uint attacker, uint defender, CombatDamageDeliveryType deliveryType);

        void ApplyDamageDealtHamstringEffect(uint attacker, uint defender, SkillType skillType, CombatDamageType damageType);

        void ApplyDamageDealtMimicryTraitProcs(uint attacker, uint defender);

        void ApplySideAttackDamageEffects(uint attacker, uint defender, SkillType skillType, int damage);

        void ApplyDamageDealtStaminaRestore(uint attacker, SkillType skillType);

        void ApplyDamageDealtAttackDelayReduction(uint attacker, SkillType skillType);

        void ApplyNextDamageDealtBleedEffect(uint attacker, uint defender, CombatDamageType damageType);

        void ApplyFrenzySlashHasteRefresh(uint attacker);

        void ApplyToxicRushDamageDealtEffects(uint attacker, uint defender, CombatDamageDeliveryType deliveryType);
    }
}
