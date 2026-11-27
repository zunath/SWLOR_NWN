using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// How a hostile ability's damage reaches its target (part 13 of the ability split,
    /// Readmes/MaintainabilityRefactorAbilitySplit.md): applied at once when no impact is tracked, otherwise queued on the impact
    /// and applied, in order, when the impact ends.
    /// </summary>
    public interface IAbilityDamageDeliveryService
    {
        /// <summary>
        /// Consumes the status-applied next-attack bonus the impact captured (once per impact), applies or queues the damage of
        /// one hostile hit, then runs what the hit's damage triggers: the Dark Force conversion,
        /// the same-target pressure bonus, the damage-dealt effects, the status effects' damage notice and, for an untracked
        /// hit, the damage reflection. The caller checks that the hit deals damage.
        /// </summary>
        void DeliverHostileImpactDamage(
            uint activator,
            uint target,
            SkillType skillType,
            int damage,
            CombatDamageType damageType,
            DamageType? effectDamageType,
            AbilityImpactTracker trackedImpact,
            bool allowsHitProcs);

        /// <summary>
        /// Applies the damage an impact queued, as one command of the activator, each hit under the recorder attribution it was
        /// queued under; a direct hit tells the status effects, sends the temporary HP feedback and reflects before it lands.
        /// </summary>
        void FlushDamageEffects(AbilityImpactTracker impact, uint activator);
    }
}
