using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;

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
        public static (FeatType, uint) DeterminePerkAbility(uint self, uint target, HashSet<uint> allies)
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
            var allyCount = allies.Count;
            var activeConcentration = Ability.GetActiveConcentration(self).Feat;

            List<FeatType> featTypes = Enum.GetValues(typeof(FeatType))
                    .Cast<FeatType>()
                    .ToList();

            var usableFeats = new List<FeatType>();

            foreach (var feat in featTypes)
            {
                try
                {
                    var ability = Ability.GetAbilityDetail(feat);
                }
                catch (Exception ex)
                {
                    continue;
                }

                if (feat == FeatType.BattleInsight1 ||
                    feat == FeatType.BattleInsight2)
                {
                    if (CheckIfCanUseFeat(self, self, feat, () => allyCount >= 1 && activeConcentration == FeatType.Invalid))
                    {
                        usableFeats.Add(feat);
                    }
                }
                else if (feat == FeatType.ForceStun1 ||
                            feat == FeatType.ForceStun2 ||
                            feat == FeatType.ForceStun3 ||
                            feat == FeatType.MindTrick1 ||
                            feat == FeatType.MindTrick2 ||
                            feat == FeatType.BurstOfSpeed1 ||
                            feat == FeatType.BurstOfSpeed2 ||
                            feat == FeatType.BurstOfSpeed3 ||
                            feat == FeatType.BurstOfSpeed4 ||
                            feat == FeatType.BurstOfSpeed5 ||
                            feat == FeatType.ForceStun1 ||
                            feat == FeatType.ForceStun2 ||
                            feat == FeatType.ForceStun3)
                {
                    if (CheckIfCanUseFeat(self, target, feat, () => activeConcentration == FeatType.Invalid))
                    {
                        usableFeats.Add(feat);
                    }
                }
                else
                {
                    if (Ability.GetAbilityDetail(feat).IsHostileAbility)
                    {
                        if (CheckIfCanUseFeat(self, target, feat))
                        {
                            usableFeats.Add(feat);
                        }
                    }
                    else
                    {
                        if (CheckIfCanUseFeat(self, self, feat))
                        {
                            usableFeats.Add(feat);
                        }
                    }
                }
            }

            if (usableFeats.Count == 0)
            {
                return (FeatType.Invalid, OBJECT_INVALID);
            }

            // randomize pick from remaining featTypes list
            var randomFeat = Service.Random.Next(usableFeats.Count);
            if (Ability.GetAbilityDetail(usableFeats[randomFeat]).IsHostileAbility)
            {
                return (usableFeats[randomFeat], target);
            }
            else
            {
                return (usableFeats[randomFeat], self);
            }
        }

        /// <summary>
        /// Checks whether a creature can use a specific feat.
        /// Verifies whether a creature has the feat, meets the condition, and can use the ability.
        /// </summary>
        /// <param name="creature">The creature to check</param>
        /// <param name="target">The target of the feat</param>
        /// <param name="feat">The feat to check</param>
        /// <param name="condition">The custom condition to check</param>
        /// <returns>true if feat can be used, false otherwise</returns>
        private static bool CheckIfCanUseFeat(uint creature, uint target, FeatType feat, Func<bool> condition = null)
        {
            if (!GetHasFeat(feat, creature)) return false;
            if (condition != null && !condition()) return false;
            if (!GetIsObjectValid(target)) return false;

            var targetLocation = GetLocation(target);
            var abilityDetail = Ability.GetAbilityDetail(feat);
            var effectiveLevel = Perk.GetEffectivePerkLevel(creature, abilityDetail.EffectiveLevelPerkType);
            return Ability.CanUseAbility(creature, target, feat, effectiveLevel, targetLocation);
        }

    }
}
