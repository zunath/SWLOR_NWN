using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.Component.StatusEffect.Model;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Events;
using SWLOR.Shared.Domain.Communication.Contracts;
using SWLOR.Shared.Domain.StatusEffect;
using SWLOR.Shared.Domain.StatusEffect.Contracts;
using SWLOR.Shared.Domain.StatusEffect.Enums;

namespace SWLOR.Component.StatusEffect.Service
{
    /// <inheritdoc/>
    public class StatusEffectService : IStatusEffectService
    {
        private const string StatusEffectTag = "STATUS_EFFECT";
        private const float Interval = 1f;

        private readonly Dictionary<uint, CreatureStatusEffect> _creatureEffects = new();

        private readonly IEventAggregator _event;
        private readonly IStatusEffectFactory _statusEffectFactory;
        private readonly IMessagingService _messagingService;

        public StatusEffectService(
            IEventAggregator eventAggregator,
            IStatusEffectFactory statusEffectFactory,
            IMessagingService messagingService)
        {
            _event = eventAggregator;
            _statusEffectFactory = statusEffectFactory;
            _messagingService = messagingService;
        }

        /// <inheritdoc/>
        public void OnModuleEnter()
        {
            var player = GetEnteringObject();
            ApplyNWNEffect(player);
        }

        private void ApplyNWNEffect(uint creature)
        {
            if (HasEffectByTag(creature, StatusEffectTag))
                return;

            var effect = EffectRunScript(
                StatusEffectScriptName.OnApplyStatusEffectScript,
                StatusEffectScriptName.OnRemoveStatusEffectScript,
                StatusEffectScriptName.OnStatusEffectIntervalScript,
                Interval);
            effect = TagEffect(effect, StatusEffectTag);
            effect = SupernaturalEffect(effect);

            ApplyEffectToObject(DurationType.Permanent, effect, creature);
        }

        /// <inheritdoc/>
        public void OnApplyNWNStatusEffect()
        {
            var creature = OBJECT_SELF;
            _creatureEffects[creature] = new CreatureStatusEffect();
        }

        /// <inheritdoc/>
        public void OnRemoveNWNStatusEffect()
        {
            var creature = OBJECT_SELF;
            if (_creatureEffects.ContainsKey(creature))
                _creatureEffects.Remove(creature);
        }

        /// <inheritdoc/>
        public void OnNWNStatusEffectInterval()
        {
            var creature = OBJECT_SELF;
            if (!_creatureEffects.ContainsKey(creature))
            {
                RemoveEffectByTag(creature, StatusEffectTag);
                return;
            }

            var effects = _creatureEffects[creature];

            foreach (var effect in effects.GetAllTickEffects())
            {
                if (effect.ActivationType != StatusEffectActivationType.Tick)
                    continue;

                if (effect.IsFlaggedForRemoval)
                {
                    RemoveStatusEffect(creature, effect.Type);
                }
                else
                {
                    effect.TickEffect(creature);
                }
            }
        }

        /// <inheritdoc/>
        public StatGroup GetCreatureStatGroup(uint creature)
        {
            var statusEffect = GetCreatureStatusEffects(creature);
            return statusEffect.StatGroup;
        }

        private CreatureStatusEffect GetCreatureStatusEffects(uint creature)
        {
            return !_creatureEffects.ContainsKey(creature)
                ? new CreatureStatusEffect()
                : _creatureEffects[creature];
        }

        /// <inheritdoc/>
        public void ApplyPermanentStatusEffect(uint source, uint creature, StatusEffectType type)
        {
            ApplyStatusEffectInternal(source, creature, type, -1, true);
        }

        private void ApplyStatusEffectInternal(uint source, uint creature, StatusEffectType type, int durationTicks, bool isPermanent)
        {
            if (!isPermanent && durationTicks <= 0)
            {
                SendMessageToPC(source, "Your spell was resisted.");
                return;
            }

            ApplyNWNEffect(creature);

            var statusEffect = _statusEffectFactory.CreateStatusEffect(type);

            var canApply = statusEffect.CanApply(creature);
            if (!string.IsNullOrWhiteSpace(canApply))
            {
                SendMessageToPC(creature, $"Effect failed to apply: {canApply}");
                return;
            }

            foreach (var morePowerful in statusEffect.MorePowerfulEffectTypes)
            {
                if (HasEffect(creature, morePowerful))
                {
                    SendMessageToPC(creature, "A more powerful effect is active on your target.");
                    return;
                }
            }

            switch (statusEffect.StackingType)
            {
                case StatusEffectStackType.Disabled:
                case StatusEffectStackType.Invalid:
                    RemoveStatusEffect(creature, type);
                    break;
                case StatusEffectStackType.StackFromMultipleSources:
                    RemoveStatusEffectInternal(creature, source, type);
                    break;
            }

            foreach (var lessPowerful in statusEffect.LessPowerfulEffectTypes)
            {
                RemoveStatusEffect(creature, lessPowerful);
            }

            _creatureEffects[creature].Add(statusEffect);
            statusEffect.ApplyEffect(source, creature, durationTicks);

            if (statusEffect.Icon != EffectIconType.Invalid)
            {
                var iconEffect = EffectIcon(statusEffect.Icon);
                iconEffect = TagEffect(iconEffect, statusEffect.Id);
                ApplyEffectToObject(DurationType.Permanent, iconEffect, creature);
            }

            if (statusEffect.SendsApplicationMessage)
            {
                var name = GetName(creature);
                var effectName = statusEffect.Name;
                var message = $"{name} receives the effect of {effectName}.";
                _messagingService.SendMessageNearbyToPlayers(creature, message);
            }
        }

        /// <inheritdoc/>
        public void ApplyStatusEffect(uint source, uint creature, StatusEffectType type, int durationTicks)
        {
            ApplyStatusEffectInternal(source, creature, type, durationTicks, false);
        }

        private void RemoveStatusEffectInternal(uint creature, uint source, StatusEffectType type)
        {
            if (!_creatureEffects.ContainsKey(creature))
                return;

            var hasSentMessage = false;
            var statusEffects = _creatureEffects[creature].GetAllEffects();
            foreach (var statusEffect in statusEffects)
            {
                if (statusEffect.Type == type)
                {
                    if (source == OBJECT_INVALID || statusEffect.Source == source)
                    {
                        if (statusEffect.SendsWornOffMessage && !hasSentMessage)
                        {
                            var name = GetName(creature);
                            var effectName = statusEffect.Name;
                            _messagingService.SendMessageNearbyToPlayers(creature, $"The effect of {effectName} wears off of {name}.");
                            hasSentMessage = true;
                        }
                    }

                    RemoveEffectByTag(creature, statusEffect.Id);
                    statusEffect.RemoveEffect(creature);
                    _creatureEffects[creature].Remove(statusEffect);
                }
            }
        }

        /// <inheritdoc/>
        public void RemoveStatusEffect(uint creature, StatusEffectType type)
        {
            RemoveStatusEffectInternal(creature, OBJECT_INVALID, type);
        }

        /// <inheritdoc/>
        public void RemoveStatusEffectBySourceType(uint creature, StatusEffectSourceType sourceType)
        {
            var creatureEffects = GetCreatureStatusEffects(creature);
            var effects = creatureEffects.GetAllBySourceType(sourceType);
            foreach (var effect in effects)
            {
                RemoveStatusEffect(creature, effect.Type);
            }
        }

        /// <inheritdoc/>
        public bool HasEffect(uint creature, StatusEffectType type)
        {
            if (!_creatureEffects.ContainsKey(creature))
                return false;

            return _creatureEffects[creature].HasEffect(type);
        }

        private void OnDealtDamage(OnDealtDamage evt)
        {
            var attacker = OBJECT_SELF;
            var effects = GetCreatureStatusEffects(attacker);

            foreach (var effect in effects.GetAllOnHitEffects())
            {
                effect.OnHitEffect(attacker, evt.Target, evt.Damage);
            }
        }
    }
}