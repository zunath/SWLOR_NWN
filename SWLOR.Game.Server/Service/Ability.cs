using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Service
{
    public static class Ability
    {
        private static readonly Dictionary<FeatType, AbilityDetail> _abilities = new();
        private static readonly Dictionary<uint, ActiveConcentrationAbility> _activeConcentrationAbilities = new();
        private static readonly Dictionary<AbilityToggleType, Action<uint, bool>> _toggleActions = new();

        /// <summary>
        /// When the module caches, abilities will be cached and events will be scheduled.
        /// </summary>
        [NWNEventHandler("mod_cache")]
        public static void CacheData()
        {
            CacheAbilities();
            CacheToggleActions();
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

        private static void CacheToggleActions()
        {
            // If more toggle actions are added, it will make sense to promote this to a full fledged builder.
            // Until then, it can live here.
            _toggleActions[AbilityToggleType.Dash] = (player, isEnabled) =>
            {
                string message;
                message = isEnabled 
                    ? ColorToken.Green("Dash enabled") 
                    : ColorToken.Red("Dash disabled");

                Stat.ApplyPlayerMovementRate(player);
                SendMessageToPC(player, message);
            };
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

            // Must have appropriate levels in the perk to use the ability.
            if (effectivePerkLevel <= 0 || ability.AbilityLevel > effectivePerkLevel)
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
            if (GetIsObjectValid(target) && !LineOfSightObject(activator, target))
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
            if (GetIsObjectValid(target) && !GetIsReactionTypeHostile(target, activator) && ability.IsHostileAbility)
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
                // Creature/target is dead or invalid.
                if (!GetIsObjectValid(creature) ||
                    GetIsDead(creature) ||
                    !GetIsObjectValid(concentrationAbility.Target) ||
                    GetIsDead(concentrationAbility.Target))
                {
                    EndConcentrationAbility(creature);
                    continue;
                }

                // Creature and caster are not in the same area.
                if (GetArea(creature) != GetArea(concentrationAbility.Target))
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

                // We don't run after activation actions until the second concentration cycle.
                // This is because if a player activates a concentration ability 1 second before the cycle,
                // they get charged for both the activation as well as the concentration cost.
                // The trade off is some abilities will last longer depending on when the player uses them in the cycle.
                // I think this is preferable to punishing the player twice though.
                if (!GetLocalBool(creature, "CONCENTRATION_FIRST_USE"))
                {
                    foreach (var req in ability.Requirements)
                    {
                        req.AfterActivationAction(creature);
                    }
                }
                DeleteLocalBool(creature, "CONCENTRATION_FIRST_USE");
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
            StatusEffect.Apply(creature, target, statusEffectType, 0.0f, null, feat);

            Messaging.SendMessageNearbyToPlayers(creature, $"{GetName(creature)} begins concentrating...");
            SetLocalBool(creature, "CONCENTRATION_FIRST_USE", true);
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
                DeleteLocalBool(creature, "CONCENTRATION_FIRST_USE");
            }
        }

        /// <summary>
        /// Returns true if the target resists the ability.
        /// Will display a combat log message indicating the roll performed.
        /// </summary>
        /// <param name="attacker">The creature performing the attack</param>
        /// <param name="defender">The creature defending against the attack</param>
        /// <param name="actionName">Name of the action or ability to display in the combat log</param>
        /// <param name="abilityType">The type of ability to check against.</param>
        public static bool GetAbilityResisted(uint attacker, uint defender, string actionName, AbilityType abilityType)
        {
            var abilityShortName = Stat.GetAbilityNameShort(abilityType);
            var attackerStat = (GetAbilityScore(attacker, abilityType) - 10) * 2.5f;
            var defenderStat = (GetAbilityScore(defender, abilityType) - 10) * 2.5f;
            var attackerRoll = d100();
            var totalAttack = attackerRoll + attackerStat;
            var isResisted = totalAttack <= defenderStat + 50;

            var operation = attackerStat < 0 ? "-" : "+";
            var coloredAttackerName = ColorToken.Custom(GetName(attacker), 153, 255, 255);
            var resistText = isResisted ? "*resist*" : "*success*";
            var message = ColorToken.Combat($"{coloredAttackerName} inflicts {actionName} on {GetName(defender)} {resistText} [{abilityShortName} vs {abilityShortName}] : ({attackerRoll} {operation} {Math.Abs(attackerStat)} = {totalAttack})");

            SendMessageToPC(attacker, message);
            SendMessageToPC(defender, message);

            return isResisted;
        }

        /// <summary>
        /// Toggles an ability on or off for a given player.
        /// If additional logic is defined in an AbilityToggleDefinition, that will be run after this is performed.
        /// </summary>
        /// <param name="player">The player to toggle on or off.</param>
        /// <param name="toggleType">The type of toggle to turn on or off.</param>
        /// <param name="isToggled">true if the ability should be enabled, false otherwise</param>
        public static void ToggleAbility(uint player, AbilityToggleType toggleType, bool isToggled)
        {
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            if (dbPlayer.AbilityToggles == null)
                dbPlayer.AbilityToggles = new Dictionary<AbilityToggleType, bool>();

            if (!dbPlayer.AbilityToggles.ContainsKey(toggleType))
                dbPlayer.AbilityToggles[toggleType] = false;

            var runLogic = dbPlayer.AbilityToggles[toggleType] != isToggled;
            dbPlayer.AbilityToggles[toggleType] = isToggled;

            DB.Set(dbPlayer);

            if (runLogic &&
                _toggleActions.ContainsKey(toggleType))
            {
                _toggleActions[toggleType](player, isToggled);
            }
        }

        /// <summary>
        /// Retrieves whether a player has a specific toggle type enabled.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="toggleType">The type of toggle to check</param>
        /// <returns>true if the ability is toggled on, false otherwise</returns>
        public static bool IsAbilityToggled(uint player, AbilityToggleType toggleType)
        {
            if (!GetIsPC(player) || GetIsDM(player))
                return false;

            var playerId = GetObjectUUID(player);
            return IsAbilityToggled(playerId, toggleType);
        }

        /// <summary>
        /// Retrieves whether  a player has a specific toggle type enabled.
        /// </summary>
        /// <param name="playerId">The player Id to check</param>
        /// <param name="toggleType">The type of toggle to check</param>
        /// <returns>true if the ability is toggled on, false otherwise</returns>
        public static bool IsAbilityToggled(string playerId, AbilityToggleType toggleType)
        {
            var dbPlayer = DB.Get<Player>(playerId);

            if (dbPlayer == null)
                return false;

            if (dbPlayer.AbilityToggles == null)
                dbPlayer.AbilityToggles = new Dictionary<AbilityToggleType, bool>();

            if (!dbPlayer.AbilityToggles.ContainsKey(toggleType))
                dbPlayer.AbilityToggles[toggleType] = false;

            return dbPlayer.AbilityToggles[toggleType];
        }

        /// <summary>
        /// Determines if any ability is toggled by a player.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <returns>true if any ability is toggled, false otherwise</returns>
        public static bool IsAnyAbilityToggled(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            if (dbPlayer == null)
                return false;

            if (dbPlayer.AbilityToggles == null)
                return false;

            foreach (var toggle in dbPlayer.AbilityToggles.Values)
            {
                if (toggle)
                    return true;
            }

            return false;
        }

    }
}
