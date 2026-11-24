using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The after-damage part of combat: the damage taken handler and its effects, low HP effects, the melee damage taken
    /// effects, avoided attacks and the effects of an attack deflection.
    /// </summary>
    public interface ICombatDamageTakenEffectsService
    {
        void ApplyDamageTakenEffects();

        void ApplyDamageTakenEffects(uint defender, uint attacker, int damage);

        void ApplyLowHPDamageTakenEffects(uint defender, int damage);

        void ApplyLowHPGuardEffectFromProtectedTarget(uint guardRecipient, uint protectedTarget, int damage);

        void TrackAvoidedAttack(uint creature, uint attacker);

        void ApplyMeleeDamageTakenEffects(uint defender, uint attacker);

        void TrackDeflection(uint creature, DeflectionSource source);

        void ApplyAbilityUsedAttackDeflection(
            uint activator,
            StatType attackDeflectionStatType,
            StatType durationStatType,
            StatType deflectionFPRestoreStatType = StatType.Invalid);

        void ApplyAbilityGrantedAttackDeflectionEffects(uint activator, DeflectionSource source);
    }
}
