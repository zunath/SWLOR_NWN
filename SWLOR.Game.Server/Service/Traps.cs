using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Tracks player-placed traps: per-owner capacity, minimum spacing between any two live traps,
    /// an arming delay, a proximity trigger, and a lifetime cap. Trap strength is snapshotted from
    /// the owner's stats at placement time.
    /// </summary>
    public static class Traps
    {
        private const int BaseTrapCapacity = 1;
        private const float MinimumSpacingMeters = 3f;
        private const float ArmingDelaySeconds = 3f;
        private const float TriggerRadiusMeters = 2f;
        private const float BlastRadiusMeters = 3f;
        private const float LifetimeSeconds = 300f;
        private const float ProximityCheckIntervalSeconds = 1f;

        private class TrapRecord
        {
            public Guid Id { get; } = Guid.NewGuid();
            public uint Owner { get; init; }
            public Location Location { get; init; }
            public bool IsLive { get; set; } = true;
        }

        private static readonly Dictionary<uint, List<TrapRecord>> _trapsByOwner = new();

        public static int GetTrapCapacity(uint owner)
        {
            return BaseTrapCapacity + Stat.GetStatAdjustment(owner, StatType.AdditionalTrapCapacity);
        }

        /// <summary>
        /// Places a trap at the target location. Placement fails when another live trap - anyone's -
        /// sits within the minimum spacing. Exceeding the owner's capacity despawns their oldest trap.
        /// </summary>
        public static bool TryPlaceTrap(
            uint owner,
            Location location,
            int baseDamage,
            CombatDamageType damageType,
            Type statusEffect,
            int statusDurationSeconds,
            VisualEffect triggerVisualEffect,
            VisualEffect markerVisualEffect)
        {
            foreach (var liveTrap in AllLiveTraps())
            {
                if (GetAreaFromLocation(liveTrap.Location) == GetAreaFromLocation(location) &&
                    GetDistanceBetweenLocations(liveTrap.Location, location) <= MinimumSpacingMeters)
                {
                    SendMessageToPC(owner, ColorToken.Red("Another trap is placed too close to that spot."));
                    return false;
                }
            }

            if (!_trapsByOwner.TryGetValue(owner, out var ownerTraps))
            {
                ownerTraps = new List<TrapRecord>();
                _trapsByOwner[owner] = ownerTraps;
            }

            ownerTraps.RemoveAll(record => !record.IsLive);
            while (ownerTraps.Count >= GetTrapCapacity(owner))
            {
                var oldest = ownerTraps[0];
                oldest.IsLive = false;
                ownerTraps.RemoveAt(0);
                SendMessageToPC(owner, "Your oldest trap deactivates as you place a new one.");
            }

            // Trap strength is snapshotted now so later gear or stance swaps do not retune a
            // trap that is already on the ground.
            var trapBonus = Stat.GetStatAdjustment(owner, StatType.TrapBonus);
            var snapshotDamage = baseDamage + (int)Math.Ceiling(baseDamage * (trapBonus / 100f));

            var record = new TrapRecord
            {
                Owner = owner,
                Location = location
            };
            ownerTraps.Add(record);

            DeviceAbilityEffects.CreateTemporaryFieldEngineerMarker(
                location,
                markerVisualEffect,
                1.5f,
                LifetimeSeconds);

            DelayCommand(ArmingDelaySeconds, () => RunProximityChecks(
                record,
                snapshotDamage,
                damageType,
                statusEffect,
                statusDurationSeconds,
                triggerVisualEffect,
                LifetimeSeconds - ArmingDelaySeconds));

            return true;
        }

        public static void ClearTraps(uint owner)
        {
            if (!_trapsByOwner.TryGetValue(owner, out var ownerTraps))
                return;

            foreach (var record in ownerTraps)
            {
                record.IsLive = false;
            }

            _trapsByOwner.Remove(owner);
        }

        private static IEnumerable<TrapRecord> AllLiveTraps()
        {
            return _trapsByOwner.Values.SelectMany(records => records).Where(record => record.IsLive);
        }

        private static void RunProximityChecks(
            TrapRecord record,
            int damage,
            CombatDamageType damageType,
            Type statusEffect,
            int statusDurationSeconds,
            VisualEffect triggerVisualEffect,
            float remainingSeconds)
        {
            if (!record.IsLive)
                return;

            if (remainingSeconds <= 0f || !GetIsObjectValid(record.Owner))
            {
                Expire(record);
                return;
            }

            var triggered = CombatAreaPulses
                .GetHostileCreatures(record.Owner, record.Location, TriggerRadiusMeters)
                .Any();

            if (triggered)
            {
                record.IsLive = false;
                CombatAreaPulses.ApplyCombatPulse(
                    record.Owner,
                    record.Location,
                    SkillType.Espionage,
                    damage,
                    BlastRadiusMeters,
                    statusEffect,
                    statusDurationSeconds,
                    damageType,
                    targetVisualEffect: triggerVisualEffect,
                    areaVisualEffect: VisualEffect.Vfx_Fnf_Smoke_Puff);
                return;
            }

            DelayCommand(ProximityCheckIntervalSeconds, () => RunProximityChecks(
                record,
                damage,
                damageType,
                statusEffect,
                statusDurationSeconds,
                triggerVisualEffect,
                remainingSeconds - ProximityCheckIntervalSeconds));
        }

        private static void Expire(TrapRecord record)
        {
            record.IsLive = false;
            if (_trapsByOwner.TryGetValue(record.Owner, out var ownerTraps))
            {
                ownerTraps.Remove(record);
            }
        }
    }
}
