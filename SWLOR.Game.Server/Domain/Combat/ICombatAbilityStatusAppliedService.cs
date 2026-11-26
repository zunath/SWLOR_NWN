using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The status-applied part of combat: the riders a status applied to the target sets off, the target status effects of a hostile
    /// ability hit, the hostile debuff party buff and the status-applied stamina drain. The part is also the listener of the status
    /// effects applied notices; its boot handler registers it.
    /// </summary>
    public interface ICombatAbilityStatusAppliedService : IStatusEffectAppliedListener
    {
        void ApplyHostileAbilityPartyBuff(uint activator, AbilityDetail ability, int damage, bool statusApplied);

        void ApplyStatusAppliedEffects(
            uint activator,
            uint target,
            StatusEffectCategory appliedStatusCategories,
            bool targetWasAttackingAlly);

        void ApplyAbilityTargetStatusEffects(uint activator, uint target, AbilityDetail ability);

        void ApplyStatusAppliedTargetStaminaDrain(
            uint activator,
            uint target,
            StatusEffectCategory appliedCategories);
    }
}
