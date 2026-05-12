using SWLOR.Game.Server.Service.AIService;
using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AIDefinition
{
    public abstract class AIBase : IAIDefinition
    {
        protected uint Self { get; private set; }
        protected uint Target { get; private set; }

        private void ResetCachedData()
        {
            Self = OBJECT_INVALID;
            Target = OBJECT_INVALID;
        }

        /// <inheritdoc />
        public virtual void PreProcessAI(uint self, uint target, List<uint> allies)
        {
            ResetCachedData();

            Self = self;
            Target = target;
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
        protected static bool CheckIfCanUseFeat(uint creature, uint target, FeatType feat, Func<bool> condition = null)
        {
            if (!GetHasFeat(feat, creature)) return false;
            if (condition != null && !condition()) return false;
            if (!GetIsObjectValid(target)) return false;

            var targetLocation = GetLocation(target);
            var abilityDetail = Ability.GetAbilityDetail(feat);
            var effectiveLevel = Perk.GetPerkLevel(creature, abilityDetail.EffectiveLevelPerkType);
            return Ability.CanUseAbility(creature, target, feat, effectiveLevel, targetLocation);
        }

        /// <inheritdoc />
        public virtual (FeatType, uint) DeterminePerkAbility()
        {
            // Note: The order is important here. The top-most abilities take precedence over lower ones.

            var (success, result) = Provoke();
            if (success) return result;

            (success, result) = NPCAbilities();
            if (success) return result;


            return NoAction.Item2;
        }

        protected static (bool, (FeatType, uint)) NoAction => (false, (FeatType.Invalid, OBJECT_INVALID));

        protected (bool, (FeatType, uint)) NPCAbilities()
        {

            // Roar
            if (CheckIfCanUseFeat(Self, Self, FeatType.Roar))
            {
                return (true, (FeatType.Roar, Self));
            }

            // Bite
            if (CheckIfCanUseFeat(Self, Target, FeatType.Bite))
            {
                return (true, (FeatType.Bite, Target));
            }

            // Iron Shell
            if (CheckIfCanUseFeat(Self, Target, FeatType.IronShell))
            {
                return (true, (FeatType.IronShell, Self));
            }

            // Screech
            if (CheckIfCanUseFeat(Self, Target, FeatType.Screech))
            {
                return (true, (FeatType.Screech, Self));
            }

            // Greater Earthquake
            if (CheckIfCanUseFeat(Self, Self, FeatType.GreaterEarthquake))
            {
                return (true, (FeatType.GreaterEarthquake, Target));
            }

            // Earthquake
            if (CheckIfCanUseFeat(Self, Self, FeatType.Earthquake))
            {
                return (true, (FeatType.Earthquake, Target));
            }

            // Flame Blast
            if (CheckIfCanUseFeat(Self, Target, FeatType.FlameBlast))
            {
                return (true, (FeatType.FlameBlast, Target));
            }

            // Fire Breath
            if (CheckIfCanUseFeat(Self, Target, FeatType.FireBreath))
            {
                return (true, (FeatType.FireBreath, Target));
            }

            // Spikes
            if (CheckIfCanUseFeat(Self, Target, FeatType.Spikes))
            {
                return (true, (FeatType.Spikes, Target));
            }

            // Venom
            if (CheckIfCanUseFeat(Self, Target, FeatType.Venom))
            {
                return (true, (FeatType.Venom, Target));
            }

            // Talon
            if (CheckIfCanUseFeat(Self, Target, FeatType.Talon))
            {
                return (true, (FeatType.Talon, Target));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) Provoke()
        {
            // Provoke
            if (CheckIfCanUseFeat(Self, Target, FeatType.Provoke2))
            {
                return (true, (FeatType.Provoke2, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.Provoke1))
            {
                return (true, (FeatType.Provoke1, Target));
            }

            return NoAction;
        }
    }
}
