using System;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// What Combat and the quests are told when a status effect is applied or refreshed (P5-5d,
    /// Readmes/MaintainabilityRefactorStatusEffectSplit.md). Character owns the interface; the implementers register themselves
    /// with <see cref="IStatusEffectService.RegisterAppliedListener"/> at boot, so the status effects notify them without holding
    /// them. A listener that has nothing to do for a notice leaves its method empty.
    /// </summary>
    public interface IStatusEffectAppliedListener
    {
        /// <summary>A status effect of these categories was applied to or refreshed on a creature by a source.</summary>
        void OnStatusApplied(uint source, uint creature, StatusEffectCategory categories);

        /// <summary>A hostile debuff was applied to or refreshed on a creature by a source, through an ability if one is active.</summary>
        void OnHostileDebuffApplied(uint source, AbilityDetail ability);

        /// <summary>An application finished and the effect was applied: the proof of the type used by the source is recorded.</summary>
        void OnApplicationCompleted(uint source, uint creature, Type statusEffectType, AbilityDetail ability);
    }
}
