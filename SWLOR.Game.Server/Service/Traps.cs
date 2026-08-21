using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
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
        private const int TrapTriggerXP = 150;

        // Concealed kit traps. Placement is hidden from everyone but the owner until an observer
        // with the matching Trapcraft training gets close enough to spot it.
        private const string ConcealedTrapResref = "espn_trap";
        private const string ConcealedTrapMarkerVariable = "ESPIONAGE_TRAP_ID";
        private const float BaseDetectionRangeMeters = 6f;
        private const int BaseDisarmChance = 50;
        private const int DisarmStatMultiplier = 2;
        private const int DisarmTierPenalty = 10;
        private const int MinDisarmChance = 5;
        private const int MaxDisarmChance = 95;

        // Espionage rank each Trapcraft tier is gated behind, used to scale disarm XP off a
        // level-vs-rank delta the same way lockboxes scale off the Slicing gates.
        private static readonly int[] TrapTierSkillRequirement = { 5, 18, 30, 45, 50 };

        // Damage and Bleed duration a kit trap deals by tier, before the placer's Trap Bonus.
        private static readonly int[] KitTrapDamageByTier = { 20, 30, 40, 52, 66 };
        private const int KitTrapStatusDurationSeconds = 30;

        private class TrapRecord
        {
            public Guid Id { get; } = Guid.NewGuid();
            public uint Owner { get; init; }
            public Location Location { get; init; }
            public uint Marker { get; set; } = OBJECT_INVALID;
            public bool IsLive { get; set; } = true;
            public int Tier { get; init; }
            public bool IsConcealed { get; init; }
            public HashSet<uint> DetectedBy { get; } = new();
        }

        private static readonly Dictionary<uint, List<TrapRecord>> _trapsByOwner = new();

        public static int GetTrapCapacity(uint owner)
        {
            return Math.Max(
                BaseTrapCapacity,
                BaseTrapCapacity + Stat.GetStatAdjustment(owner, StatType.AdditionalTrapCapacity));
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
            if (!TryReserveTrapSlot(owner, location, out var ownerTraps))
                return false;

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

            record.Marker = DeviceAbilityEffects.CreateTemporaryFieldEngineerMarker(
                location,
                markerVisualEffect,
                1.5f,
                LifetimeSeconds);

            ScheduleArming(record, snapshotDamage, damageType, statusEffect, statusDurationSeconds, triggerVisualEffect);

            return true;
        }

        /// <summary>
        /// Deploys a concealed trap from a crafted kit. Unlike the perk-active traps, the placement
        /// is hidden from everyone but the owner until an observer with the matching Trapcraft
        /// training spots it, at which point they may attempt to disarm it.
        /// </summary>
        public static bool TryPlaceKitTrap(uint owner, Location location, int tier)
        {
            if (tier < 1 || tier > KitTrapDamageByTier.Length)
                return false;

            if (!TryReserveTrapSlot(owner, location, out var ownerTraps))
                return false;

            var baseDamage = KitTrapDamageByTier[tier - 1];
            var trapBonus = Stat.GetStatAdjustment(owner, StatType.TrapBonus);
            var snapshotDamage = baseDamage + (int)Math.Ceiling(baseDamage * (trapBonus / 100f));

            var record = new TrapRecord
            {
                Owner = owner,
                Location = location,
                Tier = tier,
                IsConcealed = true
            };
            ownerTraps.Add(record);

            record.Marker = CreateObject(ObjectType.Placeable, ConcealedTrapResref, location, false, ConcealedTrapResref);
            if (GetIsObjectValid(record.Marker))
            {
                SetLocalString(record.Marker, ConcealedTrapMarkerVariable, record.Id.ToString());
                DestroyObject(record.Marker, LifetimeSeconds);

                // Hidden from the world, then revealed per-observer as detection succeeds. The
                // owner always sees their own placement.
                VisibilityPlugin.SetVisibilityOverride(OBJECT_INVALID, record.Marker, VisibilityType.Hidden);
                RevealTo(record, owner, sendMessage: false);
            }

            ScheduleArming(
                record,
                snapshotDamage,
                CombatDamageType.Physical,
                typeof(BleedStatusEffect),
                KitTrapStatusDurationSeconds,
                VisualEffect.Vfx_Com_Blood_Spark_Medium);

            return true;
        }

        // Shared spacing/capacity gate for both placement paths.
        private static bool TryReserveTrapSlot(uint owner, Location location, out List<TrapRecord> ownerTraps)
        {
            foreach (var liveTrap in AllLiveTraps())
            {
                if (GetAreaFromLocation(liveTrap.Location) == GetAreaFromLocation(location) &&
                    GetDistanceBetweenLocations(liveTrap.Location, location) <= MinimumSpacingMeters)
                {
                    SendMessageToPC(owner, ColorToken.Red("Another trap is placed too close to that spot."));
                    ownerTraps = null;
                    return false;
                }
            }

            if (!_trapsByOwner.TryGetValue(owner, out ownerTraps))
            {
                ownerTraps = new List<TrapRecord>();
                _trapsByOwner[owner] = ownerTraps;
            }

            ownerTraps.RemoveAll(record => !record.IsLive);
            while (ownerTraps.Count >= GetTrapCapacity(owner))
            {
                var oldest = ownerTraps[0];
                Deactivate(oldest);
                ownerTraps.RemoveAt(0);
                SendMessageToPC(owner, "Your oldest trap deactivates as you place a new one.");
            }

            return true;
        }

        // Trapcraft III/IV shorten the time before a placed trap goes live.
        private static void ScheduleArming(
            TrapRecord record,
            int damage,
            CombatDamageType damageType,
            Type statusEffect,
            int statusDurationSeconds,
            VisualEffect triggerVisualEffect)
        {
            var placementSpeed = Math.Clamp(Stat.GetStatAdjustment(record.Owner, StatType.TrapPlacementSpeedPercent), 0, 90);
            var armingDelay = ArmingDelaySeconds * (100 - placementSpeed) / 100f;

            DelayCommand(armingDelay, () => RunProximityChecks(
                record,
                damage,
                damageType,
                statusEffect,
                statusDurationSeconds,
                triggerVisualEffect,
                LifetimeSeconds - armingDelay));
        }

        public static void ClearTraps(uint owner)
        {
            if (!_trapsByOwner.TryGetValue(owner, out var ownerTraps))
                return;

            foreach (var record in ownerTraps)
            {
                Deactivate(record);
            }

            _trapsByOwner.Remove(owner);
        }

        // Single deactivation path so every route - eviction, clearing, triggering, expiry - both
        // marks the trap dead and removes its visible marker instead of leaving inert props behind.
        private static void Deactivate(TrapRecord record)
        {
            record.IsLive = false;
            if (GetIsObjectValid(record.Marker))
            {
                DestroyObject(record.Marker);
                record.Marker = OBJECT_INVALID;
            }
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

            if (record.IsConcealed)
            {
                RunDetectionSweep(record);
            }

            var hostileCreatures = CombatAreaPulses
                .GetHostileCreatures(record.Owner, record.Location, TriggerRadiusMeters)
                .ToList();
            var triggeredByNonPlayerCharacter = hostileCreatures.Any(creature => !GetIsPC(creature));

            if (hostileCreatures.Count > 0)
            {
                Expire(record);
                if (triggeredByNonPlayerCharacter && GetIsPC(record.Owner) && !GetIsDM(record.Owner))
                {
                    Skill.GiveSkillXP(record.Owner, SkillType.Espionage, TrapTriggerXP, false, false);
                }
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

        /// <summary>
        /// Reveals a concealed trap to nearby players trained to spot it. Trapcraft rank must meet
        /// the trap's tier, and Trap Management II widens the range at which it is noticed.
        /// </summary>
        private static void RunDetectionSweep(TrapRecord record)
        {
            if (!GetIsObjectValid(record.Marker))
                return;

            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (record.DetectedBy.Contains(player) ||
                    GetIsDM(player) ||
                    GetArea(player) != GetAreaFromLocation(record.Location))
                {
                    continue;
                }

                if (Perk.GetPerkLevel(player, PerkType.Trapcraft) < record.Tier)
                    continue;

                var range = BaseDetectionRangeMeters + Stat.GetStatAdjustment(player, StatType.TrapDetectionRangeBonus);
                if (GetDistanceBetweenLocations(GetLocation(player), record.Location) > range)
                    continue;

                RevealTo(record, player, sendMessage: true);
            }
        }

        private static void RevealTo(TrapRecord record, uint player, bool sendMessage)
        {
            if (!GetIsObjectValid(record.Marker) || !record.DetectedBy.Add(player))
                return;

            VisibilityPlugin.SetVisibilityOverride(player, record.Marker, VisibilityType.Visible);

            if (sendMessage)
            {
                SendMessageToPC(player, ColorToken.Combat("You spot a concealed trap nearby."));
            }
        }

        /// <summary>
        /// Disarm attempt, fired when a player uses a revealed concealed trap. Success clears the
        /// trap and awards Espionage XP; failure sets it off on the would-be disarmer.
        /// </summary>
        [NWNEventHandler(ScriptName.OnEspionageTrapUsed)]
        public static void DisarmTrap()
        {
            var marker = OBJECT_SELF;
            var user = GetLastUsedBy();

            if (!GetIsPC(user) || GetIsDM(user))
                return;

            var trapId = GetLocalString(marker, ConcealedTrapMarkerVariable);
            var record = AllLiveTraps().FirstOrDefault(x => x.Id.ToString() == trapId);
            if (record == null)
                return;

            if (record.Owner == user)
            {
                Deactivate(record);
                SendMessageToPC(user, "You recover your own trap.");
                return;
            }

            if (Perk.GetPerkLevel(user, PerkType.Trapcraft) < record.Tier)
            {
                SendMessageToPC(user, ColorToken.Red("You lack the Trapcraft expertise to disarm this trap."));
                return;
            }

            if (d100() <= CalculateDisarmChance(user, record.Tier))
            {
                Deactivate(record);
                GrantDisarmXP(user, record.Tier);
                if (record.Tier >= 5)
                    Achievement.GiveAchievement(user, AchievementService.AchievementType.TrapWhisperer);
                SendMessageToPC(user, "You disarm the trap.");
                return;
            }

            SendMessageToPC(user, ColorToken.Red("You set the trap off while trying to disarm it."));
            TriggerOnDisarmFailure(record, user);
        }

        /// <summary>
        /// Disarm success chance from the Disarm stat and Perception, penalized by trap tier and
        /// clamped to a sane range. Kept in one method per the single-formula rule.
        /// </summary>
        private static int CalculateDisarmChance(uint user, int tier)
        {
            var disarm = Stat.GetStatAdjustment(user, StatType.TrapDisarm);
            var perceptionModifier = GetAbilityModifier(AbilityType.Perception, user);
            var chance = BaseDisarmChance + (disarm + perceptionModifier) * DisarmStatMultiplier - tier * DisarmTierPenalty;

            return Math.Clamp(chance, MinDisarmChance, MaxDisarmChance);
        }

        private static void GrantDisarmXP(uint user, int tier)
        {
            var playerId = GetObjectUUID(user);
            var dbPlayer = DB.Get<Player>(playerId);
            var delta = TrapTierSkillRequirement[tier - 1] - dbPlayer.Skills[SkillType.Espionage].Rank;

            var xp = Skill.GetDeltaXP(delta);
            if (xp > 0)
            {
                Skill.GiveSkillXP(user, SkillType.Espionage, xp, false, false);
            }
        }

        private static void TriggerOnDisarmFailure(TrapRecord record, uint victim)
        {
            var damage = KitTrapDamageByTier[record.Tier - 1];
            Deactivate(record);

            AssignCommand(record.Owner, () =>
            {
                ApplyEffectToObject(
                    DurationType.Instant,
                    EffectDamage(damage, DamageType.Piercing),
                    victim);
            });
            StatusEffect.ApplyStatusEffect<BleedStatusEffect>(record.Owner, victim, KitTrapStatusDurationSeconds);
            ApplyEffectToObject(
                DurationType.Instant,
                EffectVisualEffect(VisualEffect.Vfx_Com_Blood_Spark_Medium),
                victim);
        }

        private static void Expire(TrapRecord record)
        {
            Deactivate(record);
            if (_trapsByOwner.TryGetValue(record.Owner, out var ownerTraps))
            {
                ownerTraps.Remove(record);
            }
        }
    }
}
