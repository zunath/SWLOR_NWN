using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.StatusEffectService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service
{
    public static class Ability
    {
        private static readonly Dictionary<FeatType, AbilityDetail> _abilities = new();

        private static readonly Dictionary<uint, ActiveConcentrationAbility> _activeConcentrationAbilities = new();

        /// <summary>
        /// When the module caches, abilities will be cached and events will be scheduled.
        /// </summary>
        [NWNEventHandler("mod_cache")]
        public static void CacheData()
        {
            CacheAbilities();
        }

        private static void CacheAbilities()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IAbilityListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                var instance = (IAbilityListDefinition) Activator.CreateInstance(type);
                var abilities = instance.BuildAbilities();

                foreach (var (feat, ability) in abilities)
                {
                    _abilities[feat] = ability;
                }
            }

            Console.WriteLine($"Loaded {_abilities.Count} abilities.");
        }

        /// <summary>
        /// Returns true if a feat is registered to an ability.
        /// Returns false otherwise.
        /// </summary>
        /// <param name="featType">The type of feat to check.</param>
        /// <returns>true if feat is registered to an ability. false otherwise.</returns>
        public static bool IsFeatRegistered(FeatType featType)
        {
            return _abilities.ContainsKey(featType);
        }

        /// <summary>
        /// Retrieves an ability's details by the specified feat type.
        /// If feat does not have an ability, an exception will be thrown.
        /// </summary>
        /// <param name="featType">The type of feat</param>
        /// <returns>The ability detail</returns>
        public static AbilityDetail GetAbilityDetail(FeatType featType)
        {
            if(!_abilities.ContainsKey(featType))
                throw new KeyNotFoundException($"Feat '{featType}' is not registered to an ability.");

            return _abilities[featType];
        }



        /// <summary>
        /// Checks whether a creature can activate the perk feat.
        /// </summary>
        /// <param name="activator">The activator of the perk feat.</param>
        /// <param name="target">The target of the perk feat.</param>
        /// <param name="abilityType">The type of ability to use.</param>
        /// <param name="effectivePerkLevel">The activator's effective perk level.</param>
        /// <param name="targetLocation">The target location of the perk feat.</param>
        /// <returns>true if successful, false otherwise</returns>
        public static bool CanUseAbility(
            uint activator,
            uint target,
            FeatType abilityType,
            int effectivePerkLevel,
            Location targetLocation)
        {
            var ability = GetAbilityDetail(abilityType);

            // Cannot use this ability in space.
            if (Space.IsPlayerInSpaceMode(activator) &&
                !ability.CanBeUsedInSpace)
            {
                SendMessageToPC(activator, "This ability cannot be used in space.");
                return false;
            }

            // Must have at least one level in the perk.
            if (effectivePerkLevel <= 0)
            {
                SendMessageToPC(activator, "You do not meet the prerequisites to use this ability.");
                return false;
            }

            // Activator is dead.
            if (GetCurrentHitPoints(activator) <= 0)
            {
                SendMessageToPC(activator, "You are dead.");
                return false;
            }

            // Not commandable
            if (!GetCommandable(activator))
            {
                SendMessageToPC(activator, "You cannot take actions at this time.");
                return false;
            }

            // Must be within line of sight.
            if (!LineOfSightObject(activator, target))
            {
                SendMessageToPC(activator, "You cannot see your target.");
                return false;
            }

            // Must not be busy
            if (Activity.IsBusy(activator))
            {
                SendMessageToPC(activator, "You are busy.");
                return false;
            }

            // Range check.
            if (GetDistanceBetween(activator, target) > ability.MaxRange)
            {
                SendMessageToPC(activator, "You are out of range.  This ability has a range of " + ability.MaxRange + " meters.");
                return false;
            }

            // Hostility check
            if (!GetIsReactionTypeHostile(target, activator) && ability.IsHostileAbility)
            {
                SendMessageToPC(activator, "You may only use this ability on enemies.");
                return false;
            }

            // Perk-specific requirement checks
            foreach (var req in ability.Requirements)
            {
                var requirementError = req.CheckRequirements(activator);
                if (!string.IsNullOrWhiteSpace(requirementError))
                {
                    SendMessageToPC(activator, requirementError);
                    return false;
                }
            }

            // Perk-specific custom validation logic.
            var customValidationResult = ability.CustomValidation == null ? string.Empty : ability.CustomValidation(activator, target, effectivePerkLevel, targetLocation);
            if (!string.IsNullOrWhiteSpace(customValidationResult))
            {
                SendMessageToPC(activator, customValidationResult);
                return false;
            }

            // Check if ability is on a recast timer still.
            var (isOnRecast, timeToWait) = Recast.IsOnRecastDelay(activator, ability.RecastGroup);
            if (isOnRecast)
            {
                SendMessageToPC(activator, $"This ability can be used in {timeToWait}.");
                return false;
            }

            return true;
        }


        /// <summary>
        /// Checks whether a creature can activate the perk feat.
        /// </summary>
        /// <param name="activator">The activator of the perk feat.</param>
        /// <param name="abilityType">The type of ability to use.</param>
        /// <returns>true if successful, false otherwise</returns>
        public static bool CanUseConcentration(
            uint activator,
            FeatType abilityType)
        {
            var ability = GetAbilityDetail(abilityType);

            // Activator is dead.
            if (GetCurrentHitPoints(activator) <= 0)
            {
                SendMessageToPC(activator, "You are dead.");
                return false;
            }

            // Not commandable
            if (!GetCommandable(activator))
            {
                SendMessageToPC(activator, "You cannot take actions at this time.");
                return false;
            }

            // Perk-specific requirement checks
            foreach (var req in ability.Requirements)
            {
                var requirementError = req.CheckRequirements(activator);
                if (!string.IsNullOrWhiteSpace(requirementError))
                {
                    SendMessageToPC(activator, requirementError);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Each tick, creatures with a concentration effect will be processed.
        /// This will drain FP and reapply whatever effect is associated with an ability.
        /// </summary>
        [NWNEventHandler("mod_heartbeat")]
        public static void ProcessConcentrationEffects()
        {
            var pairs = _activeConcentrationAbilities.ToList();

            foreach (var (creature, concentrationAbility) in pairs)
            {
                // Creature is dead or invalid.
                if (!GetIsObjectValid(creature) ||
                    GetIsDead(creature))
                {
                    EndConcentrationAbility(creature);
                    continue;
                }

                var ability = GetAbilityDetail(concentrationAbility.Feat);

                // Move to next creature if requirements aren't met.
                if (!CanUseConcentration(creature, concentrationAbility.Feat))
                {
                    EndConcentrationAbility(creature);
                    continue;
                }
                
                foreach (var req in ability.Requirements)
                {
                    req.AfterActivationAction(creature);
                }
            }
        }

        /// <summary>
        /// Starts a concentration ability on a specified creature.
        /// If there is already a concentration ability active, it will be replaced with this one.
        /// </summary>
        /// <param name="creature">The creature who will perform the concentration.</param>
        /// <param name="target">The target of the concentration effect.</param>
        /// <param name="feat">The type of ability to activate.</param>
        /// <param name="statusEffectType">The concentration status effect to apply.</param>
        public static void StartConcentrationAbility(uint creature, uint target, FeatType feat, StatusEffectType statusEffectType)
        {
            _activeConcentrationAbilities[creature] = new ActiveConcentrationAbility(target, feat, statusEffectType);
            StatusEffect.Apply(creature, target, statusEffectType, 0.0f, feat);

            Messaging.SendMessageNearbyToPlayers(creature, $"{GetName(creature)} begins concentrating...");
        }

        /// <summary>
        /// Retrieves a creature's active concentration ability.
        /// If no concentration ability is active, Feat.Invalid will be returned.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <returns>The active concentration feat or Feat.Invalid.</returns>
        public static ActiveConcentrationAbility GetActiveConcentration(uint creature)
        {
            if (_activeConcentrationAbilities.ContainsKey(creature))
            {
                return _activeConcentrationAbilities[creature];
            }

            return new ActiveConcentrationAbility(OBJECT_INVALID, FeatType.Invalid, StatusEffectType.Invalid);
        }
        
        /// <summary>
        /// Ends a concentration effect on a specified creature.
        /// If creature isn't concentrating, nothing will happen.
        /// </summary>
        /// <param name="creature"></param>
        public static void EndConcentrationAbility(uint creature)
        {
            if (_activeConcentrationAbilities.ContainsKey(creature))
            {
                var activeConcentrationEffect = _activeConcentrationAbilities[creature];
                StatusEffect.Remove(creature, activeConcentrationEffect.StatusEffectType);
                _activeConcentrationAbilities.Remove(creature);

                SendMessageToPC(creature, "You stop concentrating.");
            }
        }

        /// <summary>
        /// Returns true if the target resists the ability.
        /// </summary>
        /// <param name="activator"></param>
        /// /// <param name="target"></param>
        /// /// <param name="primaryAbilityType"></param>
        /// /// <param name="secondaryAbilityType"></param>
        public static bool GetAbilityResisted(uint activator, uint target)
        {
            if (GetAbilityModifier(AbilityType.Willpower, activator) * 10 + d100(1)
                >
                GetAbilityModifier(AbilityType.Willpower, target) * 10 + 50
                )
            {
                
                return false;
            }

            SendMessageToPC(activator, ColorToken.Gray(GetName(target)) + " resisted your ability");
            SendMessageToPC(target, ColorToken.Gray("You resisted " + GetName(activator)) + "'s ability");
            return true;
        }
    }
}
