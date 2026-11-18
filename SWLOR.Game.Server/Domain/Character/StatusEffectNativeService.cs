using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Native;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The native effect that carries each status effect and the scripts it runs (part 5 of the status effect split): building and
    /// applying the effect, the three effect-script handlers (apply, remove and interval), the ticks and the reapplication of a
    /// creature's native effects. The trackers and the removal stay in <see cref="StatusEffectService"/>; this part calls them
    /// through <see cref="IStatusEffectService"/>. The native effect read sits behind a one-line <c>protected virtual</c> method
    /// whose body is the original call. It holds no state.
    /// </summary>
    public class StatusEffectNativeService : IStatusEffectNativeService
    {
        private const string StatusEffectTag = "STATUS_EFFECT";
        private const float Interval = 1f;

        private readonly IStatusEffectService _statusEffect;

        public StatusEffectNativeService(IStatusEffectService statusEffect)
        {
            _statusEffect = statusEffect;
        }

        /// <summary>
        /// Handle when a status effect is applied (called by NWN script)
        /// </summary>
        [NWNEventHandler(ScriptName.OnApplyStatusEffect)]
        public void OnApplyStatusEffect()
        {
            OnApplyNWNStatusEffect(OBJECT_SELF);
        }

        /// <summary>
        /// Handle when a status effect is removed (called by NWN script)
        /// </summary>
        [NWNEventHandler(ScriptName.OnRemoveStatusEffect)]
        public void OnRemoveStatusEffect()
        {
            OnRemoveNWNStatusEffect(OBJECT_SELF);
        }

        /// <summary>
        /// Handle status effect interval processing (called by NWN script)
        /// </summary>
        [NWNEventHandler(ScriptName.OnStatusEffectInterval)]
        public void OnStatusEffectInterval()
        {
            OnNWNStatusEffectInterval(OBJECT_SELF);
        }

        public void OnApplyNWNStatusEffect(uint player)
        {
            _statusEffect.EnsureCreatureStatusEffectTracker(player);
        }

        public void OnRemoveNWNStatusEffect(uint player)
        {
            var tag = GetLastStatusEffectTag();
            if (string.IsNullOrWhiteSpace(tag) || tag == StatusEffectTag)
            {
                _statusEffect.RemoveCreature(player, false);

                return;
            }

            if (_statusEffect.IsRemovalSuppressed(tag))
                return;

            // NWN can deliver the old timer's removal callback after a duration extension
            // has already replaced it with another native effect carrying the same tag.
            // Wait until native removal settles, then only clear statuses without a timer.
            DelayCommand(0f, () =>
            {
                if (!MayHaveEffectTag(player, tag) || !HasEffectByTag(player, tag))
                    _statusEffect.RemoveStatusEffectById(player, tag, true, false);
            });
        }

        private string GetLastStatusEffectTag()
        {
            var effect = GetLastRunScriptEffect();
            var tag = GetEffectTag(effect);
            return !string.IsNullOrWhiteSpace(tag)
                ? tag
                : GetEffectString(effect, 0);
        }

        public void OnNWNStatusEffectInterval(uint creature)
        {
            var tag = GetLastStatusEffectTag();
            if (string.IsNullOrWhiteSpace(tag) || tag == StatusEffectTag)
            {
                TickAllStatusEffects(creature);
                return;
            }

            TickStatusEffectById(creature, tag);
        }

        private void TickAllStatusEffects(uint creature)
        {
            // Clean up invalid creatures when we encounter them
            if (!GetIsObjectValid(creature) || GetIsDead(creature))
            {
                _statusEffect.RemoveCreature(creature, false);
                RemoveEffectByTag(creature, StatusEffectTag);
                return;
            }

            if (!_statusEffect.TryGetCreatureStatusEffects(creature, out var effects))
            {
                RemoveEffectByTag(creature, StatusEffectTag);
                return;
            }

            foreach (var effect in effects.GetAllTickEffects())
            {
                if (effect.ActivationType != StatusEffectActivationType.Tick)
                    continue;

                TickStatusEffect(creature, effect);
            }
        }

        private void TickStatusEffectById(uint creature, string statusEffectId)
        {
            if (!GetIsObjectValid(creature) || GetIsDead(creature))
            {
                _statusEffect.RemoveStatusEffectById(creature, statusEffectId, false, true);
                return;
            }

            if (!_statusEffect.TryGetCreatureStatusEffects(creature, out var effects))
            {
                RemoveEffectByTag(creature, statusEffectId);
                return;
            }

            var effect = effects.GetAllTickEffects().FirstOrDefault(x => x.Id == statusEffectId);
            if (effect == null)
            {
                RemoveEffectByTag(creature, statusEffectId);
                return;
            }

            TickStatusEffect(creature, effect);
        }

        private void TickStatusEffect(uint creature, IStatusEffect effect)
        {
            if (effect.ActivationType != StatusEffectActivationType.Tick)
                return;

            if (effect.IsFlaggedForRemoval)
            {
                _statusEffect.RemoveStatusEffectById(creature, effect.Id, true, true);
            }
            else
            {
                effect.TickEffect(creature);

                if (effect.IsFlaggedForRemoval)
                {
                    _statusEffect.RemoveStatusEffectById(creature, effect.Id, true, true);
                }
            }
        }

        public void ReapplyNWNEffects(uint creature, CreatureStatusEffect effects)
        {
            var shouldReapplyMovementRate = false;

            foreach (var statusEffect in effects.GetAllEffects())
            {
                statusEffect.ReapplyEffect(creature);
                ApplyTrackedNWNEffect(creature, statusEffect, statusEffect.DurationTicks, statusEffect.DurationTicks < 0);

                if (_statusEffect.HasStatAdjustment(statusEffect))
                {
                    shouldReapplyMovementRate = true;
                }
            }

            if (shouldReapplyMovementRate)
            {
                _statusEffect.ApplyCreatureMovementRate(creature);
            }
        }

        public void ApplyTrackedNWNEffect(uint creature, IStatusEffect statusEffect, int durationTicks, bool isPermanent)
        {
            if (MayHaveEffectTag(creature, statusEffect.Id) && HasEffectByTag(creature, statusEffect.Id))
                return;

            ApplyNativeStatusEffect(creature, statusEffect, durationTicks, isPermanent);
        }

        // RemoveEffectByTag hides every effect it finds with the tag, so the presence check that
        // ApplyTrackedNWNEffect makes would always fail here.
        public void ReplaceNativeStatusEffect(uint creature, IStatusEffect statusEffect, int durationTicks, bool isPermanent)
        {
            _statusEffect.RemoveNativeStatusEffect(creature, statusEffect.Id);
            ApplyNativeStatusEffect(creature, statusEffect, durationTicks, isPermanent);
        }

        private void ApplyNativeStatusEffect(uint creature, IStatusEffect statusEffect, int durationTicks, bool isPermanent)
        {
            var effect = BuildNativeStatusEffect(statusEffect);

            if (isPermanent || durationTicks < 0)
            {
                ApplyEffectToObject(DurationType.Permanent, effect, creature);
            }
            else
            {
                var durationSeconds = GetStatusEffectDurationSeconds(statusEffect, durationTicks);
                ApplyEffectToObject(DurationType.Temporary, effect, creature, durationSeconds);
            }

            _statusEffect.ScheduleTemporaryHitPointLimitCheckForVitality(creature, statusEffect);
        }

        private Effect BuildNativeStatusEffect(IStatusEffect statusEffect)
        {
            var intervalScript = statusEffect.ActivationType == StatusEffectActivationType.Tick
                ? ScriptName.OnStatusEffectInterval
                : string.Empty;

            var effect = EffectRunScript(
                ScriptName.OnApplyStatusEffect,
                ScriptName.OnRemoveStatusEffect,
                intervalScript,
                Interval,
                statusEffect.Id);
            effect = TagEffect(effect, statusEffect.Id);

            if (statusEffect.Icon != EffectIconType.Invalid)
            {
                var iconEffect = TagEffect(EffectIcon(statusEffect.Icon), statusEffect.Id);
                effect = LinkEffect(effect, iconEffect);
            }

            var abilityStatEffects = BuildAbilityStatEffects(statusEffect);
            effect = LinkEffect(effect, abilityStatEffects);
            effect = TagEffect(effect, statusEffect.Id);
            return SupernaturalEffect(effect);
        }

        private float GetStatusEffectDurationSeconds(IStatusEffect statusEffect, int durationTicks)
        {
            var logicalDurationSeconds = statusEffect.DurationTicks > 0
                ? statusEffect.GetRemainingDurationSeconds(Clock.UtcNow)
                : durationTicks * Math.Max(1f, statusEffect.Frequency);

            // NWN may remove an effect before delivering an interval callback scheduled for the
            // exact same timestamp. Ticking effects therefore keep one logical tick of native
            // lifetime grace so a late callback can catch up and the final logical tick can remove
            // the effect.
            // Passive effects have no interval callback and retain their exact duration.
            return statusEffect.ActivationType == StatusEffectActivationType.Tick
                ? logicalDurationSeconds + Math.Max(Interval, statusEffect.Frequency)
                : logicalDurationSeconds;
        }

        private Effect LinkEffect(Effect linkedEffect, Effect effect)
        {
            return effect == null
                ? linkedEffect
                : EffectLinkEffects(linkedEffect, effect);
        }

        private Effect BuildAbilityStatEffects(IStatusEffect statusEffect)
        {
            Effect linkedEffect = null;

            foreach (var (ability, amount) in statusEffect.StatGroup.Abilities)
            {
                if (ability == AbilityType.Invalid || amount == 0)
                    continue;

                var effect = amount > 0
                    ? EffectAbilityIncrease(ability, amount)
                    : EffectAbilityDecrease(ability, Math.Abs(amount));
                effect = TagEffect(effect, statusEffect.Id);

                linkedEffect = linkedEffect == null
                    ? effect
                    : EffectLinkEffects(linkedEffect, effect);
            }

            return linkedEffect;
        }

        // A native read of the object's applied effects.
        protected virtual bool MayHaveEffectTag(uint creature, string tag) => AppliedEffects.MayHaveTag(creature, tag);
    }
}
