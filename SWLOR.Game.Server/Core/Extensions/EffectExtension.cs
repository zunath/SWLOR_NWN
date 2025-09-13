using System;
using System.Linq;
using SWLOR.NWN.API.NWScript.Enum;

// ReSharper disable once CheckNamespace
namespace SWLOR.Game.Server.Core.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Removes all effects with the specified tag(s) from a creature.
        /// </summary>
        /// <param name="creature">The creature to remove effects from.</param>
        /// <param name="tags">The tags to look for.</param>
        public static void RemoveEffectByTag(uint creature, params string[] tags)
        {
            for (var effect = GetFirstEffect(creature); GetIsEffectValid(effect); effect = GetNextEffect(creature))
            {
                var effectTag = GetEffectTag(effect);
                if (tags.Contains(effectTag))
                {
                    RemoveEffect(creature, effect);
                }
            }
        }

        /// <summary>
        /// Removes all effects with the specified types from a creature.
        /// </summary>
        /// <param name="creature">The creature to remove from.</param>
        /// <param name="types">The types of effects to look for.</param>
        public static void RemoveEffect(uint creature, params EffectTypeScript[] types)
        {
            for (var effect = GetFirstEffect(creature); GetIsEffectValid(effect); effect = GetNextEffect(creature))
            {
                var type = GetEffectType(effect);
                if (types.Contains(type))
                {
                    RemoveEffect(creature, effect);
                }
            }
        }

        /// <summary>
        /// Determines if creature has at least one effect with the specified tags.
        /// </summary>
        /// <param name="creature">The creature to check</param>
        /// <param name="tags">The effect tags to check for</param>
        /// <returns>true if at least one effect was found, false otherwise</returns>
        public static bool HasEffectByTag(uint creature, params string[] tags)
        {
            for (var effect = GetFirstEffect(creature); GetIsEffectValid(effect); effect = GetNextEffect(creature))
            {
                var effectTag = GetEffectTag(effect);
                if (tags.Contains(effectTag))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if a creature has a more powerful effect active based on the provided effect tag/level mapping provided.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <param name="tier">The tier attempting to be applied</param>
        /// <param name="effectLevels">The tag/level mapping of all levels.</param>
        /// <returns>true if a more powerful effect is in place, false otherwise</returns>
        public static bool HasMorePowerfulEffect(uint creature, int tier, params (string, int)[] effectLevels)
        {
            for (var effect = GetFirstEffect(creature); GetIsEffectValid(effect); effect = GetNextEffect(creature))
            {
                var tag = GetEffectTag(effect);
                foreach (var (levelTag, level) in effectLevels)
                {
                    if (tag == levelTag && tier < level)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
