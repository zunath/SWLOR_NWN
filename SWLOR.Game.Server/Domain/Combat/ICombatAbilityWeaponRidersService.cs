using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The weapon family riders of a successful ability hit (what a skill's perks add to the target), the Spear Disabler suppression
    /// riders, and the perk category enmity and guarded hit statuses an ability hit applies. The ability impact part picks the family.
    /// </summary>
    public interface ICombatAbilityWeaponRidersService
    {
        void ApplyHeavyVibrobladeDefenseImpactRiders(uint activator, uint target, AbilityDetail ability);

        void ApplyForceDarkImpactRiders(
            uint activator,
            uint target,
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects);

        void ApplyKatarVenomCurrentImpactRiders(uint activator, uint target);

        void ApplyLightsaberOffenseImpactRiders(uint activator, uint target, AbilityDetail ability);

        void ApplyPistolSkirmisherImpactRiders(uint activator, uint target, AbilityDetail ability);

        void ApplyRiflePacificationImpactRiders(uint activator, uint target);

        void ApplySaberstaffConduitImpactRiders(uint activator, uint target, AbilityDetail ability);

        void ApplySaberstaffTempestImpactRiders(uint activator, uint target, AbilityDetail ability);

        void ApplySpearDamageImpactRiders(uint activator, uint target, AbilityDetail ability);

        void ApplySpearDisablerImpactRiders(
            uint activator,
            uint target,
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects);

        void ApplySpearDisablerSuppressionRiders(uint activator, uint target);

        void ApplyStaffCrusherImpactRiders(uint activator, uint target);

        void ApplyThrowingDeadeyeImpactRiders(uint activator, uint target, AbilityDetail ability);

        void ApplyTwinBladeDuelistImpactRiders(uint activator, uint target, AbilityDetail ability);

        void ApplyVibroknifeShadowImpactRiders(uint activator, uint target, AbilityDetail ability);

        void ApplyVibroknifeSaboteurImpactRiders(
            uint activator,
            uint target,
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects);

        void ApplyAbilityUsedPerkCategoryTargetEnmityToSourceStatus(uint activator, uint target, AbilityDetail ability);

        void ApplyGuardedHitNextSkillAbilityExposedStatus(uint activator, uint target, SkillType skillType);
    }
}
