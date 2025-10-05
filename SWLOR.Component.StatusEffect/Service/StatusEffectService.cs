using SWLOR.Component.StatusEffect.Model;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Combat.Events;
using SWLOR.Shared.Domain.Communication.Contracts;
using SWLOR.Shared.Domain.StatusEffect;
using SWLOR.Shared.Domain.StatusEffect.Contracts;
using SWLOR.Shared.Domain.StatusEffect.Enums;
using SWLOR.Shared.Domain.StatusEffect.Events;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Component.StatusEffect.Service
{
    public class StatusEffectService
    {
        private const string StatusEffectTag = "STATUS_EFFECT";
        private const float Interval = 1f;

        private readonly Dictionary<uint, CreatureStatusEffect> _creatureEffects = new();

        private readonly IEventAggregator _event;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMessagingService _messagingService;

        public StatusEffectService(
            IEventAggregator eventAggregator,
            IServiceProvider serviceProvider,
            IMessagingService messagingService)
        {
            _event = eventAggregator;
            _serviceProvider = serviceProvider;
            _messagingService = messagingService;

            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            _event.Subscribe<OnApplyStatusEffect>(OnApplyNWNStatusEffect);
            _event.Subscribe<OnRemoveStatusEffect>(OnRemoveNWNStatusEffect);
            _event.Subscribe<OnStatusEffectInterval>(OnNWNStatusEffectInterval);

            _event.Subscribe<OnModuleEnter>(OnPlayerEnter);
            _event.Subscribe<OnDealtDamage>(OnDealtDamage);
        }

        private void OnPlayerEnter(OnModuleEnter evt)
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

        private void OnApplyNWNStatusEffect(OnApplyStatusEffect evt)
        {
            var creature = OBJECT_SELF;
            _creatureEffects[creature] = new CreatureStatusEffect();
        }

        private void OnRemoveNWNStatusEffect(OnRemoveStatusEffect evt)
        {
            var creature = OBJECT_SELF;
            if (_creatureEffects.ContainsKey(creature))
                _creatureEffects.Remove(creature);
        }

        private void OnNWNStatusEffectInterval(OnStatusEffectInterval evt)
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
                    RemoveStatusEffect(effect.GetType(), creature);
                }
                else
                {
                    effect.TickEffect(creature);
                }
            }
        }

        public CreatureStatusEffect GetCreatureStatusEffects(uint creature)
        {
            return !_creatureEffects.ContainsKey(creature) 
                ? new CreatureStatusEffect() 
                : _creatureEffects[creature];
        }

        public void ApplyPermanentStatusEffect<T>(uint source, uint creature)
            where T: IStatusEffect
        {
            ApplyStatusEffectInternal(typeof(T), source, creature, -1, true);
        }

        public void ApplyPermanentStatusEffect(Type type, uint source, uint creature)
        {
            ApplyStatusEffectInternal(type, source, creature, -1, true);
        }

        private void ApplyStatusEffectInternal(Type type, uint source, uint creature, int durationTicks, bool isPermanent)
        {
            if (!isPermanent && durationTicks <= 0)
            {
                SendMessageToPC(source, "Your spell was resisted.");
                return;
            }

            ApplyNWNEffect(creature);

            var statusEffect = (IStatusEffect)_serviceProvider.GetService(type);

            var canApply = statusEffect.CanApply(creature);
            if (!string.IsNullOrWhiteSpace(canApply))
            {
                SendMessageToPC(creature, $"Effect failed to apply: {canApply}");
                return;
            }

            foreach (var morePowerful in statusEffect.MorePowerfulEffectTypes)
            {
                if (HasEffect(morePowerful, creature))
                {
                    SendMessageToPC(creature, "A more powerful effect is active on your target.");
                    return;
                }
            }

            switch (statusEffect.StackingType)
            {
                case StatusEffectStackType.Disabled:
                case StatusEffectStackType.Invalid:
                    RemoveStatusEffect(type, creature);
                    break;
                case StatusEffectStackType.StackFromMultipleSources:
                    RemoveStatusEffect(type, creature, source);
                    break;
            }

            foreach (var lessPowerful in statusEffect.LessPowerfulEffectTypes)
            {
                RemoveStatusEffect(lessPowerful, creature);
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

        public void ApplyStatusEffect<T>(uint source, uint creature, int durationTicks)
            where T: IStatusEffect
        {
            var type = typeof(T);
            ApplyStatusEffectInternal(type, source, creature, durationTicks, false);
        }

        private void RemoveStatusEffect(Type type, uint creature, uint source)
        {
            if (!_creatureEffects.ContainsKey(creature))
                return;

            var hasSentMessage = false;
            var statusEffects = _creatureEffects[creature].GetAllEffects();
            foreach (var statusEffect in statusEffects)
            {
                if (statusEffect.GetType() == type)
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

        public void RemoveStatusEffect<T>(uint creature)
            where T: IStatusEffect
        {
            var type = typeof(T);
            RemoveStatusEffect(type, creature);
        }

        public void RemoveStatusEffect(Type type, uint creature)
        {
            RemoveStatusEffect(type, creature, OBJECT_INVALID);
        }

        public void RemoveStatusEffectBySourceType(uint creature, StatusEffectSourceType sourceType)
        {
            var creatureEffects = GetCreatureStatusEffects(creature);
            var effects = creatureEffects.GetAllBySourceType(sourceType);
            foreach (var effect in effects)
            {
                RemoveStatusEffect(effect.GetType(), creature);
            }
        }

        public bool HasEffect(Type type, uint creature)
        {
            if (!_creatureEffects.ContainsKey(creature))
                return false;

            return _creatureEffects[creature].HasEffect(type);
        }

        public bool HasEffect<T>(uint creature)
            where T : IStatusEffect
        {
            return HasEffect(typeof(T), creature);
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