using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.StatusEffectService
{
    public static class StatusEffectService
    {
        private const string StatusEffectTag = "STATUS_EFFECT";
        private const float Interval = 1f;

        private static readonly Dictionary<uint, CreatureStatusEffect> _creatureEffects = new();

        public static void OnPlayerEnter(uint module)
        {
            var player = GetEnteringObject();
            ApplyNWNEffect(player);
        }

        private static void ApplyNWNEffect(uint creature)
        {
            if (HasEffectByTag(creature, StatusEffectTag))
                return;

            var effect = EffectRunScript(
                "status_apply",
                "status_remove", 
                "status_interval",
                Interval);
            effect = TagEffect(effect, StatusEffectTag);
            effect = SupernaturalEffect(effect);

            ApplyEffectToObject(DurationType.Permanent, effect, creature);
        }

        public static void OnApplyNWNStatusEffect(uint player)
        {
            _creatureEffects[player] = new CreatureStatusEffect();
        }

        public static void OnRemoveNWNStatusEffect(uint player)
        {
            if (_creatureEffects.ContainsKey(player))
                _creatureEffects.Remove(player);
        }

        public static void OnNWNStatusEffectInterval(uint creature)
        {
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

        public static CreatureStatusEffect GetCreatureStatusEffects(uint creature)
        {
            return !_creatureEffects.ContainsKey(creature) 
                ? new CreatureStatusEffect() 
                : _creatureEffects[creature];
        }

        public static void ApplyPermanentStatusEffect<T>(uint source, uint creature)
            where T: IStatusEffect
        {
            ApplyStatusEffectInternal(typeof(T), source, creature, -1, true);
        }

        public static void ApplyPermanentStatusEffect(Type type, uint source, uint creature)
        {
            ApplyStatusEffectInternal(type, source, creature, -1, true);
        }

        private static void ApplyStatusEffectInternal(Type type, uint source, uint creature, int durationTicks, bool isPermanent)
        {
            if (!isPermanent && durationTicks <= 0)
            {
                SendMessageToPC(source, "Your spell was resisted.");
                return;
            }

            ApplyNWNEffect(creature);

            var statusEffect = (IStatusEffect)Activator.CreateInstance(type);

            var canApply = statusEffect.CanApply(creature);
            if (!string.IsNullOrWhiteSpace(canApply))
            {
                var message = $"Effect failed to apply: {canApply}";
                SendMessageToPC(creature, message);
                return;
            }

            foreach (var morePowerful in statusEffect.MorePowerfulEffectTypes)
            {
                if (HasEffect(morePowerful, creature))
                {
                    var message = "A more powerful effect is active.";
                    SendMessageToPC(creature, message);
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
                Messaging.SendMessageNearbyToPlayers(creature,
                    $"{name} receives the effect of {effectName}");
            }
        }

        public static void ApplyStatusEffect<T>(uint source, uint creature, int durationTicks)
            where T: IStatusEffect
        {
            var type = typeof(T);
            ApplyStatusEffectInternal(type, source, creature, durationTicks, false);
        }

        private static void RemoveStatusEffect(Type type, uint creature, uint source)
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
                            Messaging.SendMessageNearbyToPlayers(creature,
                                $"{name}'s {effectName} effect has worn off.");

                            hasSentMessage = true;
                        }
                    }

                    RemoveEffectByTag(creature, statusEffect.Id);
                    statusEffect.RemoveEffect(creature);
                    _creatureEffects[creature].Remove(statusEffect);
                }
            }
        }

        public static void RemoveStatusEffect<T>(uint creature)
            where T: IStatusEffect
        {
            var type = typeof(T);
            RemoveStatusEffect(type, creature);
        }

        public static void RemoveStatusEffect(Type type, uint creature)
        {
            RemoveStatusEffect(type, creature, OBJECT_INVALID);
        }

        public static void RemoveStatusEffectBySourceType(uint creature, StatusEffectSourceType sourceType)
        {
            var creatureEffects = GetCreatureStatusEffects(creature);
            var effects = creatureEffects.GetAllBySourceType(sourceType);
            foreach (var effect in effects)
            {
                RemoveStatusEffect(effect.GetType(), creature);
            }
        }

        public static bool HasEffect(Type type, uint creature)
        {
            if (!_creatureEffects.ContainsKey(creature))
                return false;

            return _creatureEffects[creature].HasEffect(type);
        }

        public static bool HasEffect<T>(uint creature)
            where T : IStatusEffect
        {
            return HasEffect(typeof(T), creature);
        }

        private static void OnChangeJobs(uint player)
        {
            if (!_creatureEffects.ContainsKey(player))
                return;

            foreach (var effect in _creatureEffects[player].GetAllEffects())
            {
                if (effect.IsRemovedOnJobChange)
                {
                    RemoveStatusEffect(effect.GetType(), player);
                }
            }
        }

        public static void OnCreatureDealtDamage(uint attacker)
        {
            var effects = GetCreatureStatusEffects(attacker);

            foreach (var effect in effects.GetAllOnHitEffects())
            {
                // For OnHit effects, we need to get the target of the attack
                // This is a simplified approach - in a real implementation, you'd need to
                // track the target from the attack event
                var target = OBJECT_INVALID; // Placeholder - would need proper target tracking
                if (GetIsObjectValid(target))
                {
                    // Get damage amount - this would need to be adapted to SWLOR's damage system
                    var damage = 0; // Placeholder - would need to get actual damage
                    effect.OnHitEffect(attacker, target, damage);
                }
            }
        }

        public static void ProcessHeartbeat()
        {
            // Process all status effects that need ticking
            var creaturesToProcess = new List<uint>();
            
            foreach (var creature in _creatureEffects.Keys)
            {
                if (GetIsObjectValid(creature) && !GetIsDead(creature))
                {
                    creaturesToProcess.Add(creature);
                }
            }

            foreach (var creature in creaturesToProcess)
            {
                OnNWNStatusEffectInterval(creature);
            }
        }

        public static void RemoveAllStatusEffects(uint creature)
        {
            if (!_creatureEffects.ContainsKey(creature))
                return;

            var effects = _creatureEffects[creature].GetAllEffects();
            foreach (var effect in effects)
            {
                RemoveStatusEffect(effect.GetType(), creature);
            }
        }
    }
}
