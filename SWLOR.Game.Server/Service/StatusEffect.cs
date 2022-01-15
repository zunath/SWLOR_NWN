using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.StatusEffectService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service
{
    public static class StatusEffect
    {
        private class StatusEffectGroup
        {
            public uint Source { get; set; }
            public DateTime Expiration { get; set; }
            public FeatType ConcentrationFeatType { get; set; }
            public object EffectData { get; set; }
        }

        private static readonly Dictionary<StatusEffectType, StatusEffectDetail> _statusEffects = new();
        private static readonly Dictionary<uint, Dictionary<StatusEffectType, StatusEffectGroup>> _creaturesWithStatusEffects = new();
        private static readonly Dictionary<uint, Dictionary<StatusEffectType, StatusEffectGroup>> _loggedOutPlayersWithEffects = new();

        /// <summary>
        /// When the module loads, cache all status effects.
        /// </summary>
        [NWNEventHandler("mod_cache")]
        public static void CacheStatusEffects()
        {
            // Organize perks to make later reads quicker.
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IStatusEffectListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                var instance = (IStatusEffectListDefinition)Activator.CreateInstance(type);
                var statusEffects = instance.BuildStatusEffects();

                foreach (var (statusEffectType, detail) in statusEffects)
                {
                    _statusEffects[statusEffectType] = detail;
                }
            }
        }

        /// <summary>
        /// Gives a status effect to a creature.
        /// If creature already has the status effect, and their timer is shorter than length,
        /// it will be extended to the length specified.
        /// </summary>
        /// <param name="source">The source of the status effect.</param>
        /// <param name="target">The creature receiving the status effect.</param>
        /// <param name="statusEffectType">The type of status effect to give.</param>
        /// <param name="length">The amount of time, in seconds, the status effect should last. Set to 0.0f to make it permanent.</param>
        /// <param name="effectData">Effect data used by the effect.</param>
        /// <param name="concentrationFeatType">If status effect is associated with a concentration ability, this will track the feat type used.</param>
        public static void Apply(
            uint source, 
            uint target, 
            StatusEffectType statusEffectType, 
            float length, 
            object effectData = null,
            FeatType concentrationFeatType = FeatType.Invalid)
        {
            if (!_creaturesWithStatusEffects.ContainsKey(target))
                _creaturesWithStatusEffects[target] = new Dictionary<StatusEffectType, StatusEffectGroup>();

            if (!_creaturesWithStatusEffects[target].ContainsKey(statusEffectType))
                _creaturesWithStatusEffects[target][statusEffectType] = new StatusEffectGroup();

            var expiration = length == 0.0f ? DateTime.MaxValue : DateTime.UtcNow.AddSeconds(length);

            // If the existing status effect will expire later than this, exit early.
            if (_creaturesWithStatusEffects[target].ContainsKey(statusEffectType))
            {
                if (_creaturesWithStatusEffects[target][statusEffectType].Expiration > expiration)
                    return;
            }

            // Set the group details.
            _creaturesWithStatusEffects[target][statusEffectType].Source = source;
            _creaturesWithStatusEffects[target][statusEffectType].Expiration = expiration;
            _creaturesWithStatusEffects[target][statusEffectType].ConcentrationFeatType = concentrationFeatType;
            _creaturesWithStatusEffects[target][statusEffectType].EffectData = effectData;

            // Run the Grant Action, if applicable.
            var statusEffectDetail = _statusEffects[statusEffectType];
            statusEffectDetail.AppliedAction?.Invoke(source, target, length, effectData);

            // Add the status effect icon if there is one.
            if (statusEffectDetail.EffectIconId > 0)
            {
                ObjectPlugin.AddIconEffect(target, statusEffectDetail.EffectIconId);
            }

            Messaging.SendMessageNearbyToPlayers(target, $"{GetName(target)} receives the effect of {statusEffectDetail.Name}.");
        }

        /// <summary>
        /// When a player enters the server, if any of their status effects in limbo, re-add them to the
        /// dictionary for processing.
        /// </summary>
        [NWNEventHandler("mod_enter")]
        public static void PlayerEnter()
        {
            var player = GetEnteringObject();

            if (!_loggedOutPlayersWithEffects.ContainsKey(player))
                return;

            var effects = _loggedOutPlayersWithEffects[player].ToDictionary(x => x.Key, y => y.Value);
            _creaturesWithStatusEffects[player] = effects;

            _loggedOutPlayersWithEffects.Remove(player);
        }

        /// <summary>
        /// When a player leaves the server, move their status effects to a different dictionary
        /// so they aren't processed unnecessarily.  
        /// </summary>
        [NWNEventHandler("mod_exit")]
        public static void PlayerExit()
        {
            var player = GetExitingObject();

            if (!_creaturesWithStatusEffects.ContainsKey(player))
                return;

            var effects = _creaturesWithStatusEffects[player].ToDictionary(x => x.Key, y => y.Value);
            _loggedOutPlayersWithEffects[player] = effects;

            _creaturesWithStatusEffects.Remove(player);
        }

        /// <summary>
        /// When the module heartbeat runs, execute and clean up status effects on all creatures.
        /// </summary>
        [NWNEventHandler("mod_heartbeat")]
        public static void TickStatusEffects()
        {
            var now = DateTime.UtcNow;

            foreach (var (creature, statusEffects) in _creaturesWithStatusEffects)
            {
                // Creature is dead or invalid. Remove its status effects.
                var removeAllEffects = !GetIsObjectValid(creature) || GetIsDead(creature);

                // Iterate over each status effect, cleaning them up if they've expired or executing their tick if applicable.
                foreach (var (statusEffect, group) in statusEffects)
                {
                    var activeConcentration = Ability.GetActiveConcentration(group.Source);

                    // Concentration check - If caster is no longer channeling this feat, remove the status effect.
                    if (group.ConcentrationFeatType != FeatType.Invalid)
                    {
                        if (activeConcentration.Feat != group.ConcentrationFeatType)
                        {
                            Remove(creature, statusEffect);
                            continue;
                        }
                    }

                    // Status effect has expired or creature is no longer valid. Remove it.
                    if (removeAllEffects || now > group.Expiration)
                    {
                        Remove(creature, statusEffect);

                        // Concentration - End the ability if this status effect was tied to a concentration ability
                        // and the creature was the target.
                        if (group.ConcentrationFeatType != FeatType.Invalid &&
                            activeConcentration.Feat == group.ConcentrationFeatType &&
                            activeConcentration.Target == creature)
                        {
                            Ability.EndConcentrationAbility(group.Source);
                        }

                    }
                    // Otherwise do a Tick.
                    else
                    {
                        var detail = _statusEffects[statusEffect];
                        detail.TickAction?.Invoke(group.Source, creature, group.EffectData);
                    }
                }

                // No more status effects. Remove the creature from the cache.
                if (statusEffects.Count <= 0)
                {
                    _creaturesWithStatusEffects.Remove(creature);
                }
            }
        }

        /// <summary>
        /// When a player dies, remove any status effects which are present.
        /// </summary>
        [NWNEventHandler("mod_death")]
        public static void OnPlayerDeath()
        {
            var player = GetLastPlayerDied();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            if (!_creaturesWithStatusEffects.ContainsKey(player))
                return;

            var statusEffects = _creaturesWithStatusEffects[player].Select(s => s.Key);

            foreach (var effect in statusEffects)
            {
                Remove(player, effect);
            }
        }

        /// <summary>
        /// Removes a status effect from a creature.
        /// </summary>
        /// <param name="creature">The creature to remove the status effect from.</param>
        /// <param name="statusEffectType">The type of status effect to remove.</param>
        public static void Remove(uint creature, StatusEffectType statusEffectType)
        {
            if (!HasStatusEffect(creature, statusEffectType, true)) return;

            var effectInstance = _creaturesWithStatusEffects[creature][statusEffectType];
            _creaturesWithStatusEffects[creature].Remove(statusEffectType);

            var statusEffectDetail = _statusEffects[statusEffectType];
            statusEffectDetail.RemoveAction?.Invoke(creature, effectInstance.EffectData);

            if (statusEffectDetail.EffectIconId > 0 && GetIsObjectValid(creature))
            {
                ObjectPlugin.RemoveIconEffect(creature, statusEffectDetail.EffectIconId);
            }

            Messaging.SendMessageNearbyToPlayers(creature, $"{GetName(creature)}'s {statusEffectDetail.Name} effect has worn off.");
        }

        /// <summary>
        /// Checks if a creature has a status effect.
        /// If ignoreExpiration is true, even if the effect is expired this will return true.
        /// This should only be used within this class to avoid confusion.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <param name="statusEffectType">The status effect type to look for.</param>
        /// <param name="ignoreExpiration">If true, expired effects will return true. Otherwise, expiration will be checked.</param>
        /// <returns>true if creature has status effect, false otherwise</returns>
        private static bool HasStatusEffect(uint creature, StatusEffectType statusEffectType, bool ignoreExpiration)
        {
            // Creature doesn't exist in the cache.
            if (!_creaturesWithStatusEffects.ContainsKey(creature))
                return false;

            // Status effect doesn't exist for this creature in the cache.
            if (!_creaturesWithStatusEffects[creature].ContainsKey(statusEffectType))
                return false;

            // Status effect has expired, but hasn't cleaned up yet.
            if (!ignoreExpiration)
            {
                var now = DateTime.UtcNow;
                if (now > _creaturesWithStatusEffects[creature][statusEffectType].Expiration)
                    return false;
            }

            // Status effect hasn't expired.
            return true;
        }

        /// <summary>
        /// Checks if a creature has a status effect.
        /// If no status effect types are specified, false will be returned.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <param name="statusEffectTypes">The status effect types to look for.</param>
        /// <returns>true if creature has status effect, false otherwise</returns>
        public static bool HasStatusEffect(uint creature, params StatusEffectType[] statusEffectTypes)
        {
            foreach (var statusEffectType in statusEffectTypes)
            {
                if (HasStatusEffect(creature, statusEffectType, false))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Retrieves the effect data associated with a creature's effect.
        /// If creature does not have effect, the default value of T will be returned.
        /// </summary>
        /// <typeparam name="T">The type of data to retrieve from the effect.</typeparam>
        /// <param name="creature">The creature to check.</param>
        /// <param name="effectType">The type of effect.</param>
        /// <returns>An effect data object or a default object of type T</returns>
        public static T GetEffectData<T>(uint creature, StatusEffectType effectType)
        {
            if (!_creaturesWithStatusEffects.ContainsKey(creature) ||
                !_creaturesWithStatusEffects[creature].ContainsKey(effectType))
                return default;

            return (T)_creaturesWithStatusEffects[creature][effectType].EffectData;
        }
    }
}
