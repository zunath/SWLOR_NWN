using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.AbilityService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service
{
    public static class Ability
    {
        private static readonly Dictionary<Feat, AbilityDetail> _abilities = new Dictionary<Feat, AbilityDetail>();

        // Recast Group Descriptions
        private static readonly Dictionary<RecastGroup, string> _recastDescriptions = new Dictionary<RecastGroup, string>();

        /// <summary>
        /// When the module loads, abilities will be cached.
        /// </summary>
        [NWNEventHandler("mod_load")]
        public static void BuildCaches()
        {
            CacheRecastGroupNames();
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
        }

        /// <summary>
        /// Returns true if a feat is registered to an ability.
        /// Returns false otherwise.
        /// </summary>
        /// <param name="featType">The type of feat to check.</param>
        /// <returns>true if feat is registered to an ability. false otherwise.</returns>
        public static bool IsFeatRegistered(Feat featType)
        {
            return _abilities.ContainsKey(featType);
        }

        /// <summary>
        /// Retrieves an ability's details by the specified feat type.
        /// If feat does not have an ability, an exception will be thrown.
        /// </summary>
        /// <param name="featType">The type of feat</param>
        /// <returns>The ability detail</returns>
        public static AbilityDetail GetAbilityDetail(Feat featType)
        {
            if(!_abilities.ContainsKey(featType))
                throw new KeyNotFoundException($"Feat '{featType}' is not registered to an ability.");

            return _abilities[featType];
        }

        /// <summary>
        /// Reads all of the enum values on the RecastGroup enumeration and stores their short name into the cache.
        /// </summary>
        private static void CacheRecastGroupNames()
        {
            foreach (var recast in Enum.GetValues(typeof(RecastGroup)).Cast<RecastGroup>())
            {
                var attr = recast.GetAttribute<RecastGroup, RecastGroupAttribute>();
                _recastDescriptions[recast] = attr.ShortName;
            }
        }

        /// <summary>
        /// Retrieves the human-readable name of a recast group.
        /// </summary>
        /// <param name="recastGroup">The recast group to retrieve.</param>
        /// <returns>The name of a recast group.</returns>
        public static string GetRecastGroupName(RecastGroup recastGroup)
        {
            if (!_recastDescriptions.ContainsKey(recastGroup))
                throw new KeyNotFoundException($"Recast group {recastGroup} has not been registered. Did you forget the Description attribute?");

            return _recastDescriptions[recastGroup];
        }


        /// <summary>
        /// Checks whether a creature can activate the perk feat.
        /// </summary>
        /// <param name="activator">The activator of the perk feat.</param>
        /// <param name="target">The target of the perk feat.</param>
        /// <param name="abilityType">The type of ability to use.</param>
        /// <param name="effectivePerkLevel">The activator's effective perk level.</param>
        /// <returns>true if successful, false otherwise</returns>
        public static bool CanUseAbility(uint activator, uint target, Feat abilityType, int effectivePerkLevel)
        {
            var ability = GetAbilityDetail(abilityType);

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
            var customValidationResult = ability.CustomValidation == null ? string.Empty : ability.CustomValidation(activator, target, effectivePerkLevel);
            if (!string.IsNullOrWhiteSpace(customValidationResult))
            {
                SendMessageToPC(activator, customValidationResult);
                return false;
            }

            // Check if ability is on a recast timer still.
            if (IsOnRecastDelay(activator, ability.RecastGroup))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns true if a recast delay has not expired yet.
        /// Returns false if there is no recast delay or the time has already passed.
        /// </summary>
        /// <param name="creature">The creature to check</param>
        /// <param name="recastGroup">The recast group to check</param>
        /// <returns>true if recast delay hasn't passed. false otherwise</returns>
        private static bool IsOnRecastDelay(uint creature, RecastGroup recastGroup)
        {
            if (GetIsDM(creature)) return false;
            var now = DateTime.UtcNow;

            // Players
            if (GetIsPC(creature) && !GetIsDMPossessed(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = DB.Get<Entity.Player>(playerId);

                if (!dbPlayer.RecastTimes.ContainsKey(recastGroup)) return false;

                if (now >= dbPlayer.RecastTimes[recastGroup])
                {
                    return false;
                }
                else
                {
                    string timeToWait = Time.GetTimeToWaitLongIntervals(now, dbPlayer.RecastTimes[recastGroup], false);
                    SendMessageToPC(creature, $"This ability can be used in {timeToWait}.");
                    return true;
                }
            }
            // NPCs and DM-possessed NPCs
            else
            {
                string unlockDate = GetLocalString(creature, $"ABILITY_RECAST_ID_{(int)recastGroup}");
                if (string.IsNullOrWhiteSpace(unlockDate))
                {
                    return false;
                }
                else
                {
                    var dateTime = DateTime.ParseExact(unlockDate, "yyyy-MM-dd hh:mm:ss", CultureInfo.InvariantCulture);
                    if (now >= dateTime)
                    {
                        return false;
                    }
                    else
                    {
                        string timeToWait = Time.GetTimeToWaitLongIntervals(now, dateTime, false);
                        SendMessageToPC(creature, $"This ability can be used in {timeToWait}.");
                        return true;
                    }
                }
            }
        }

    }
}
