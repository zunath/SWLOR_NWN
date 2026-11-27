using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// Temporary immunities after a control effect (part 19 of the ability split, Readmes/MaintainabilityRefactorAbilitySplit.md),
    /// moved from <c>AbilityService</c> unchanged: the native immunity effects by tag, the shared hard crowd control window and
    /// the check for a hard crowd control status in effect, which reads the status effects.
    /// </summary>
    public class AbilityImmunityService : IAbilityImmunityService
    {
        private readonly IStatusEffectService _statusEffect;

        public AbilityImmunityService(IStatusEffectService statusEffect)
        {
            _statusEffect = statusEffect;
        }

        /// <summary>
        /// The hard-CC immunity types which, in addition to their own same-type immunity,
        /// also grant and check immunity against every other type in this set. This lets a
        /// target who was just knocked down (for example) resist being chained into a daze,
        /// stun, immobilize, blind, sleep, or confusion for the same window.
        /// </summary>
        private static readonly FrozenSet<ImmunityType> HardCrowdControlImmunityTypes = new HashSet<ImmunityType>
        {
            ImmunityType.Knockdown,
            ImmunityType.Dazed,
            ImmunityType.Stun,
            ImmunityType.Immobilized,
            ImmunityType.Blindness,
            ImmunityType.Sleep,
            ImmunityType.Confused
        }.ToFrozenSet();

        /// <summary>
        /// Applies a temporary immunity effect to a particular target.
        /// This will add 20 seconds on top of whatever the ability duration length is.
        /// An existing immunity of the same type is replaced so its timer restarts from this
        /// application without shortening a longer remaining immunity.
        /// If the immunity is one of the hard-CC types, this also grants temporary immunity
        /// to every other hard-CC type for the same duration.
        /// </summary>
        /// <param name="target">The target receiving the immunity</param>
        /// <param name="abilityDuration">The length of the ability's duration. This will be added on top of the 20 seconds.</param>
        /// <param name="immunity">The type of immunity to apply.</param>
        public void ApplyTemporaryImmunity(uint target, float abilityDuration, ImmunityType immunity)
        {
            ApplyTemporaryImmunityForDuration(
                target,
                TemporaryImmunityBaseDurationSeconds + abilityDuration,
                immunity);
        }

        /// <summary>
        /// Applies the remaining post-control immunity after accounting for time that elapsed
        /// while the affected creature was logged out.
        /// </summary>
        public void ApplyPostControlImmunity(
            uint target,
            float secondsSinceControlEnded,
            ImmunityType immunity)
        {
            var duration = Math.Max(
                0f,
                TemporaryImmunityBaseDurationSeconds - Math.Max(0f, secondsSinceControlEnded));
            if (duration <= 0f)
                return;

            ApplyTemporaryImmunityForDuration(target, duration, immunity);
        }

        private const float TemporaryImmunityBaseDurationSeconds = 20f;

        private void ApplyTemporaryImmunityForDuration(
            uint target,
            float duration,
            ImmunityType immunity)
        {
            ApplyTemporaryImmunitySingle(target, duration, immunity);

            if (HardCrowdControlImmunityTypes.Contains(immunity))
            {
                ApplyTemporaryImmunitySingle(target, duration, ImmunityType.HardCrowdControl);
            }
        }

        private void ApplyTemporaryImmunitySingle(uint target, float requestedDuration, ImmunityType immunity)
        {
            var effectTag = GetTemporaryImmunityEffectTag(immunity);
            var duration = Math.Max(
                requestedDuration,
                GetTemporaryImmunityDurationRemaining(target, effectTag));
            if (duration <= 0f)
                return;

            RemoveEffectByTag(target, effectTag);
            var effect = EffectImmunity(immunity);
            effect = TagEffect(effect, effectTag);
            ApplyEffectToObject(DurationType.Temporary, effect, target, duration);
        }

        private int GetTemporaryImmunityDurationRemaining(uint target, string effectTag)
        {
            var remaining = 0;
            for (var effect = GetFirstEffect(target);
                 GetIsEffectValid(effect);
                 effect = GetNextEffect(target))
            {
                if (GetEffectTag(effect) == effectTag)
                {
                    remaining = Math.Max(remaining, GetEffectDurationRemaining(effect));
                }
            }

            return remaining;
        }

        public bool HasTemporaryImmunity(uint target, ImmunityType immunity)
        {
            return HasEffectByTag(target, GetTemporaryImmunityEffectTag(immunity));
        }

        /// <summary>
        /// Checks whether the target is immune to a hard-CC type: it still has immunity to that
        /// specific type, it recently suffered a different hard-CC type and is still within the
        /// shared post-control immunity window, or a hard-CC status is active on it right now -
        /// controls do not stack, they queue behind the post-control window.
        /// </summary>
        public bool HasHardCrowdControlImmunity(uint target, ImmunityType immunity)
        {
            return HasTemporaryImmunity(target, immunity) ||
                   HasTemporaryImmunity(target, ImmunityType.HardCrowdControl) ||
                   HasActiveHardCrowdControlStatus(target);
        }

        private bool HasActiveHardCrowdControlStatus(uint target)
        {
            return _statusEffect.GetCreatureStatusEffects(target)
                .GetAllEffects()
                .Any(effect =>
                    (effect.Categories & StatusEffectCategory.HardCrowdControl) ==
                    StatusEffectCategory.HardCrowdControl);
        }

        private string GetTemporaryImmunityEffectTag(ImmunityType immunity)
        {
            return $"ABILITY_TEMP_IMMUNITY_{immunity}";
        }
    }
}
