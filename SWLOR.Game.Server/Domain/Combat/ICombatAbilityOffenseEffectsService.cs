using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The offense riders of an ability activation: the movement speed, the area ability rewards, the hostile ability Force
    /// Attack, attack adjustment, sequence and resource restore riders, and the next auto-attack damage bonus. The hostile ability
    /// sequence per activator is the part's own table.
    /// </summary>
    public interface ICombatAbilityOffenseEffectsService : ICombatCreatureStateOwner
    {
        void ApplyAreaAbilityUsedRewards(uint activator, AbilityDetail ability, bool isAreaAbility);

        void ApplyAbilityUsedMovementSpeed(uint activator, AbilityDetail ability, SkillType skillType);

        void ApplyHostileAbilityForceAttack(uint activator, AbilityDetail ability);

        void ApplyHostileAbilityFPSpendForceAttack(uint activator, AbilityDetail ability);

        void ApplyHostileAbilityUsedAttackAdjustment(uint activator, AbilityDetail ability);

        void ApplyHostileAbilitySequenceEffects(uint activator, FeatType feat, AbilityDetail ability);

        void ApplyHostileAbilityResourceRestoreEffects(uint activator, AbilityDetail ability);

        void ApplyNextAutoAttackDamageBonus(uint activator, StatType bonusStatType, StatType durationStatType);

        void ForgetActivationState(uint creature);
    }
}
