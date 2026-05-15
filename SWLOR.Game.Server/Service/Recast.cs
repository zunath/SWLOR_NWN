using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service
{
    public static class Recast
    {
        // Recast Group Descriptions
        private static readonly Dictionary<RecastGroup, string> _recastDescriptions = new Dictionary<RecastGroup, string>();
        public const int MaximumReductionPercent = 50;

        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheRecastGroups()
        {
            CacheRecastGroupNames();
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
        /// Returns true if a recast delay has not expired yet.
        /// Returns false if there is no recast delay or the time has already passed.
        /// </summary>
        /// <param name="creature">The creature to check</param>
        /// <param name="recastGroup">The recast group to check</param>
        /// <returns>true if recast delay hasn't passed. false otherwise. If true, also returns a string containing a user-readable amount of time they need to wait. Otherwise it will be an empty string.</returns>
        public static (bool, string) IsOnRecastDelay(uint creature, RecastGroup recastGroup)
        {
            if (GetIsDM(creature)) return (false, string.Empty);
            var now = DateTime.UtcNow;

            // Players
            if (GetIsPC(creature) && !GetIsDMPossessed(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = DB.Get<Player>(playerId);

                if (!dbPlayer.RecastTimes.ContainsKey(recastGroup)) return (false, string.Empty);

                var timeToWait = Time.GetTimeToWaitLongIntervals(now, dbPlayer.RecastTimes[recastGroup], false);
                return (now < dbPlayer.RecastTimes[recastGroup], timeToWait);
            }
            // NPCs and DM-possessed NPCs
            else
            {
                var unlockDate = GetLocalString(creature, $"ABILITY_RECAST_ID_{(int)recastGroup}");
                if (string.IsNullOrWhiteSpace(unlockDate))
                {
                    return (false, string.Empty);
                }
                else
                {
                    var dateTime = DateTime.ParseExact(unlockDate, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    var timeToWait = Time.GetTimeToWaitLongIntervals(now, dateTime, false);
                    return (now < dateTime, timeToWait);
                }
            }
        }

        /// <summary>
        /// Applies a recast delay on a specific recast group.
        /// If group is invalid or delay amount is less than or equal to zero, nothing will happen.
        /// </summary>
        /// <param name="activator">The activator of the ability.</param>
        /// <param name="group">The recast group to put this delay under.</param>
        /// <param name="delaySeconds">The number of seconds to delay.</param>
        /// <param name="ignoreRecastReduction">If true, recast reduction bonuses are ignored.</param>
        public static void ApplyRecastDelay(uint activator, RecastGroup group, float delaySeconds, bool ignoreRecastReduction)
        {
            if (!GetIsObjectValid(activator) || group == RecastGroup.Invalid || delaySeconds <= 0.0f) return;

            // NPCs and DM-possessed NPCs
            if (!GetIsPC(activator) || GetIsDMPossessed(activator))
            {
                var recastDate = DateTime.UtcNow.AddSeconds(delaySeconds);
                var recastDateString = recastDate.ToString("yyyy-MM-dd HH:mm:ss");
                SetLocalString(activator, $"ABILITY_RECAST_ID_{(int)group}", recastDateString);
            }
            // Players
            else if (GetIsPC(activator) && !GetIsDM(activator))
            {
                var playerId = GetObjectUUID(activator);
                var dbPlayer = DB.Get<Player>(playerId);

                if (!ignoreRecastReduction)
                {
                    var recastPercentage = GetRecastReductionPercent(activator, dbPlayer) * 0.01f;

                    delaySeconds -= delaySeconds * recastPercentage;
                }



                var recastDate = DateTime.UtcNow.AddSeconds(delaySeconds);
                dbPlayer.RecastTimes[group] = recastDate;

                DB.Set(dbPlayer);
            }

        }

        public static int GetRecastReductionPercent(uint activator)
        {
            if (!GetIsPC(activator) || GetIsDM(activator) || GetIsDMPossessed(activator))
                return 0;

            var playerId = GetObjectUUID(activator);
            var dbPlayer = DB.Get<Player>(playerId);

            return GetRecastReductionPercent(activator, dbPlayer);
        }

        private static int GetRecastReductionPercent(uint activator, Player dbPlayer)
        {
            var recastReduction = dbPlayer.AbilityRecastReduction +
                                  Stat.GetStatAdjustment(activator, StatType.AbilityRecastReductionPercent);

            return Math.Clamp(recastReduction, 0, MaximumReductionPercent);
        }

        public static void ReduceRecastDelay(uint activator, RecastGroup group, float reduceSeconds)
        {
            if (!GetIsObjectValid(activator) || group == RecastGroup.Invalid || reduceSeconds <= 0f)
                return;

            var now = DateTime.UtcNow;

            if (!GetIsPC(activator) || GetIsDMPossessed(activator))
            {
                var localName = $"ABILITY_RECAST_ID_{(int)group}";
                var unlockDate = GetLocalString(activator, localName);
                if (string.IsNullOrWhiteSpace(unlockDate))
                    return;

                var dateTime = DateTime.ParseExact(unlockDate, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                if (dateTime <= now)
                {
                    DeleteLocalString(activator, localName);
                    return;
                }

                var reducedDate = dateTime.AddSeconds(-reduceSeconds);
                if (reducedDate <= now)
                {
                    DeleteLocalString(activator, localName);
                }
                else
                {
                    SetLocalString(activator, localName, reducedDate.ToString("yyyy-MM-dd HH:mm:ss"));
                }
            }
            else if (GetIsPC(activator) && !GetIsDM(activator))
            {
                var playerId = GetObjectUUID(activator);
                var dbPlayer = DB.Get<Player>(playerId);

                if (!dbPlayer.RecastTimes.TryGetValue(group, out var unlockDate))
                    return;

                if (unlockDate <= now)
                {
                    dbPlayer.RecastTimes.Remove(group);
                    DB.Set(dbPlayer);
                    return;
                }

                var reducedDate = unlockDate.AddSeconds(-reduceSeconds);
                if (reducedDate <= now)
                {
                    dbPlayer.RecastTimes.Remove(group);
                }
                else
                {
                    dbPlayer.RecastTimes[group] = reducedDate;
                }

                DB.Set(dbPlayer);
            }
        }
    }
}
