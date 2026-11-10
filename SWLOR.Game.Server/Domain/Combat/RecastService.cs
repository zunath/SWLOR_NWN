using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The recast groups' names and visibility, and the cooldown deadlines of players and NPCs. The logic that used to be
    /// static in <c>Service.Recast</c> is moved here unchanged; the name tables are this instance's own fields, filled
    /// by the module cache handler and read per ability use without being rebuilt. <c>RecastTimestamp</c> and
    /// <c>Time</c> are pure utilities and stay static. The event handler runs on the container's instance; static callers
    /// reach it through <c>GameServices.Recast</c>.
    /// </summary>
    public class RecastService : IRecastService
    {
        // Recast Group Descriptions
        private readonly Dictionary<RecastGroup, string> _recastDescriptions = new Dictionary<RecastGroup, string>();
        private readonly Dictionary<RecastGroup, string> _recastNames = new Dictionary<RecastGroup, string>();
        private readonly HashSet<RecastGroup> _visibleRecastGroups = new HashSet<RecastGroup>();

        private readonly IDBService _db;
        private readonly IAbilityCooldownVisualService _cooldownVisual;

        public RecastService(IDBService db, IAbilityCooldownVisualService cooldownVisual)
        {
            _db = db;
            _cooldownVisual = cooldownVisual;
        }

        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public void CacheRecastGroups()
        {
            CacheRecastGroupNames();
        }

        /// <summary>
        /// Reads all of the enum values on the RecastGroup enumeration and stores their short name into the cache.
        /// </summary>
        private void CacheRecastGroupNames()
        {
            _recastDescriptions.Clear();
            _recastNames.Clear();
            _visibleRecastGroups.Clear();

            foreach (var recast in Enum.GetValues(typeof(RecastGroup)).Cast<RecastGroup>())
            {
                var attr = recast.GetAttribute<RecastGroup, RecastGroupAttribute>();
                _recastDescriptions[recast] = attr.ShortName;
                _recastNames[recast] = attr.Name;

                if (attr.IsVisible)
                {
                    _visibleRecastGroups.Add(recast);
                }
            }
        }

        public string GetRecastGroupName(RecastGroup recastGroup)
        {
            if (!_recastDescriptions.ContainsKey(recastGroup))
                throw new KeyNotFoundException($"Recast group {recastGroup} has not been registered. Did you forget the Description attribute?");

            return _recastDescriptions[recastGroup];
        }

        public string GetRecastGroupDisplayName(RecastGroup recastGroup)
        {
            if (!_recastNames.ContainsKey(recastGroup))
                throw new KeyNotFoundException($"Recast group {recastGroup} has not been registered. Did you forget the Description attribute?");

            return _recastNames[recastGroup];
        }

        public bool IsRecastGroupVisible(RecastGroup recastGroup)
        {
            return _visibleRecastGroups.Contains(recastGroup);
        }

        public (bool, string) IsOnRecastDelay(uint creature, RecastGroup recastGroup)
        {
            if (GetIsDM(creature)) return (false, string.Empty);
            var now = Clock.UtcNow;

            // Players
            if (GetIsPC(creature) && !GetIsDMPossessed(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _db.Get<Player>(playerId);
                if (dbPlayer.RecastTimes == null)
                {
                    dbPlayer.RecastTimes = new Dictionary<RecastGroup, DateTime>();
                    _db.Set(dbPlayer);
                    return (false, string.Empty);
                }

                if (!dbPlayer.RecastTimes.ContainsKey(recastGroup)) return (false, string.Empty);

                var timeToWait = Time.GetTimeToWaitLongIntervals(now, dbPlayer.RecastTimes[recastGroup], false);
                return (now < dbPlayer.RecastTimes[recastGroup], timeToWait);
            }
            // NPCs and DM-possessed NPCs
            else
            {
                if (!TryGetNpcRecastTime(creature, recastGroup, out var dateTime))
                {
                    return (false, string.Empty);
                }
                else
                {
                    var timeToWait = Time.GetTimeToWaitLongIntervals(now, dateTime, false);
                    return (now < dateTime, timeToWait);
                }
            }
        }

        public void ApplyRecastDelay(uint activator, RecastGroup group, float delaySeconds)
        {
            if (!GetIsObjectValid(activator) || group == RecastGroup.Invalid || delaySeconds <= 0f) return;
            var now = Clock.UtcNow;
            ApplyRecastDelay(activator, group, now, now.AddSeconds(delaySeconds));
        }

        public void ApplyRecastDelay(uint activator, RecastGroup group, DateTime startedAt, DateTime endsAt,
            string sourceTexture = null, string iconTexture = null)
        {
            if (!GetIsObjectValid(activator) || group == RecastGroup.Invalid || endsAt <= Clock.UtcNow || endsAt <= startedAt) return;
            if (!GetIsPC(activator) || GetIsDMPossessed(activator))
            {
                SetLocalString(activator, $"ABILITY_RECAST_ID_{(int)group}", RecastTimestamp.Format(endsAt));
            }
            else if (!GetIsDM(activator))
            {
                var dbPlayer = _db.Get<Player>(GetObjectUUID(activator));
                dbPlayer.RecastTimes ??= new Dictionary<RecastGroup, DateTime>();
                dbPlayer.RecastTimes[group] = endsAt;
                _db.Set(dbPlayer);
                _cooldownVisual.ApplyRecastDelay(activator, group, startedAt, endsAt, sourceTexture, iconTexture);
            }
        }

        public void ReduceRecastDelay(uint activator, RecastGroup group, float reduceSeconds)
        {
            if (!GetIsObjectValid(activator) || group == RecastGroup.Invalid || reduceSeconds <= 0f)
                return;

            if (group == RecastGroup.Capstone)
                return;

            var now = Clock.UtcNow;

            if (!GetIsPC(activator) || GetIsDMPossessed(activator))
            {
                var localName = $"ABILITY_RECAST_ID_{(int)group}";
                if (!TryGetNpcRecastTime(activator, group, out var dateTime))
                    return;

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
                    SetLocalString(activator, localName, RecastTimestamp.Format(reducedDate));
                }
            }
            else if (GetIsPC(activator) && !GetIsDM(activator))
            {
                var playerId = GetObjectUUID(activator);
                var dbPlayer = _db.Get<Player>(playerId);
                if (dbPlayer.RecastTimes == null)
                {
                    dbPlayer.RecastTimes = new Dictionary<RecastGroup, DateTime>();
                    _db.Set(dbPlayer);
                    return;
                }

                if (!dbPlayer.RecastTimes.TryGetValue(group, out var unlockDate))
                    return;

                if (unlockDate <= now)
                {
                    dbPlayer.RecastTimes.Remove(group);
                    _db.Set(dbPlayer);
                    _cooldownVisual.ClearRecastDelay(activator, group);
                    return;
                }

                var reducedDate = unlockDate.AddSeconds(-reduceSeconds);
                if (reducedDate <= now)
                {
                    dbPlayer.RecastTimes.Remove(group);
                    _cooldownVisual.ClearRecastDelay(activator, group);
                }
                else
                {
                    dbPlayer.RecastTimes[group] = reducedDate;
                    _cooldownVisual.RefreshRecastDelay(activator, group, reducedDate);
                }

                _db.Set(dbPlayer);
            }
        }

        private bool TryGetNpcRecastTime(uint creature, RecastGroup group, out DateTime endsAt)
        {
            var localName = $"ABILITY_RECAST_ID_{(int)group}";
            var value = GetLocalString(creature, localName);
            if (RecastTimestamp.TryParse(value, out endsAt))
                return true;

            // A malformed local must not interrupt the creature's ability processing.
            if (!string.IsNullOrEmpty(value))
                DeleteLocalString(creature, localName);

            return false;
        }
    }
}
