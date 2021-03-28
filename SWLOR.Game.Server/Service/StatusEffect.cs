using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
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
        }

        private static readonly Dictionary<StatusEffectType, StatusEffectDetail> _statusEffects = new Dictionary<StatusEffectType, StatusEffectDetail>();
        private static readonly Dictionary<uint, Dictionary<StatusEffectType, StatusEffectGroup>> _creaturesWithStatusEffects = new Dictionary<uint, Dictionary<StatusEffectType, StatusEffectGroup>>();

        /// <summary>
        /// When the module loads, cache all status effects.
        /// </summary>
        [NWNEventHandler("mod_load")]
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
        /// <param name="creature">The creature receiving the status effect.</param>
        /// <param name="statusEffectType">The type of status effect to give.</param>
        /// <param name="length">The amount of time the status effect should last. Set to 0.0f to make it permanent.</param>
        public static void Apply(uint source, uint creature, StatusEffectType statusEffectType, float length)
        {
            if(!_creaturesWithStatusEffects.ContainsKey(creature))
                _creaturesWithStatusEffects[creature] = new Dictionary<StatusEffectType, StatusEffectGroup>();

            if(!_creaturesWithStatusEffects[creature].ContainsKey(statusEffectType))
                _creaturesWithStatusEffects[creature][statusEffectType] = new StatusEffectGroup();

            var expiration = length == 0.0f ? DateTime.MaxValue : DateTime.UtcNow.AddSeconds(length);

            // If the existing status effect will expire later than this, exit early.
            if (_creaturesWithStatusEffects[creature].ContainsKey(statusEffectType))
            {
                if (_creaturesWithStatusEffects[creature][statusEffectType].Expiration > expiration)
                    return;
            }

            // Set the group details.
            _creaturesWithStatusEffects[creature][statusEffectType].Source = source;
            _creaturesWithStatusEffects[creature][statusEffectType].Expiration = expiration;

            // Run the Grant Action, if applicable.
            var statusEffectDetail = _statusEffects[statusEffectType];
            statusEffectDetail.AppliedAction?.Invoke(source, creature, length);

            // Add the status effect icon if there is one.
            if (statusEffectDetail.EffectIconId > 0)
            {
                Core.NWNX.Object.AddIconEffect(creature, statusEffectDetail.EffectIconId);
            }

            Messaging.SendMessageNearbyToPlayers(creature, $"{GetName(creature)} receives the effect of {statusEffectDetail.Name}.");
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
                bool removeAllEffects = !GetIsObjectValid(creature) || GetIsDead(creature);

                // Iterate over each status effect, cleaning them up if they've expired or executing their tick if applicable.
                foreach (var (statusEffect, group) in statusEffects)
                {
                    // Status effect has expired or creature is no longer valid. Remove it.
                    if (removeAllEffects || now > group.Expiration)
                    {
                        Remove(creature, statusEffect);
                    }
                    // Otherwise do a Tick.
                    else
                    {
                        var detail = _statusEffects[statusEffect];
                        detail.TickAction?.Invoke(group.Source, creature);
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
        /// Removes a status effect from a creature.
        /// </summary>
        /// <param name="creature">The creature to remove the status effect from.</param>
        /// <param name="statusEffectType">The type of status effect to remove.</param>
        public static void Remove(uint creature, StatusEffectType statusEffectType)
        {
            if (!HasStatusEffect(creature, statusEffectType,  true)) return;
            _creaturesWithStatusEffects[creature].Remove(statusEffectType);

            var statusEffectDetail = _statusEffects[statusEffectType];
            statusEffectDetail.RemoveAction?.Invoke(creature);

            if (statusEffectDetail.EffectIconId > 0)
            {
                Core.NWNX.Object.RemoveIconEffect(creature, statusEffectDetail.EffectIconId);
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
        /// Returns the source of a status effect which was applied onto a target creature.
        /// If the status effect cannot be found, OBJECT_INVALID will be returned.
        /// If source cannot be determined, OBJECT_INVALID will be returned.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <param name="statusEffectType">The status effect type to look for.</param>
        /// <returns>The source of a status effect, or OBJECT_INVALID if it cannot be determined.</returns>
        public static uint GetSource(uint creature, StatusEffectType statusEffectType)
        {
            if (!HasStatusEffect(creature, statusEffectType)) return OBJECT_INVALID;
            return _creaturesWithStatusEffects[creature][statusEffectType].Source;
        }

    }
}
