using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.AIDefinition
{
    public static class GenericAIDefinition
    {
        /// <summary>
        /// Determines which perk ability to use.
        /// </summary>
        /// <param name="self">The creature</param>
        /// <param name="target">The creature's target</param>
        /// <param name="allies">Allies associated with this creature. Should also include this creature.</param>
        /// <returns>A feat and target</returns>
        public static (Feat, uint) DeterminePerkAbility(uint self, uint target, HashSet<uint> allies)
        {
            static float CalculateAverageHP(uint creature)
            {
                var currentHP = GetCurrentHitPoints(creature);
                var maxHP = GetMaxHitPoints(creature);
                return ((float)currentHP / (float)maxHP) * 100;
            }

            var hpPercentage = CalculateAverageHP(self);

            var lowestHPAlly = allies.OrderBy(CalculateAverageHP).First();
            var allyHPPercentage = CalculateAverageHP(lowestHPAlly);
            var selfRace = GetRacialType(self);
            var lowestHPAllyRace = GetRacialType(lowestHPAlly);

            return (Feat.Invalid, OBJECT_INVALID);
        }

        /// <summary>
        /// Checks whether a creature can use a specific feat.
        /// Verifies whether a creature has the feat, meets the condition, and can use the ability.
        /// </summary>
        /// <param name="creature">The creature to check</param>
        /// <param name="target">The target of the feat</param>
        /// <param name="feat">The feat to check</param>
        /// <param name="perkType">The type of perk to check</param>
        /// <param name="condition">The custom condition to check</param>
        /// <returns>true if feat can be used, false otherwise</returns>
        private static bool CheckIfCanUseFeat(uint creature, uint target, Feat feat, PerkType perkType, Func<bool> condition = null)
        {
            if (!GetHasFeat(feat, creature)) return false;
            if (condition != null && !condition()) return false;
            if (!GetIsObjectValid(target)) return false;

            var effectiveLevel = Perk.GetEffectivePerkLevel(creature, perkType);
            return Ability.CanUseAbility(creature, target, feat, effectiveLevel);
        }

    }
}
