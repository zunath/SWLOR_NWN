using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;

namespace SWLOR.Game.Server.Feature.AbilityDefinition
{
    /// <summary>
    /// Combat rules shared by Resuscitation and Revive Beast. Revives fail during PvP fights, and in PvE
    /// each player can be revived mid-fight at most once every 5 minutes. Validation runs before any cost
    /// is paid, so a failed attempt spends nothing.
    /// </summary>
    public static class ReviveRules
    {
        public const float HostilePlayerRangeMeters = 20f;
        public const float PartyDamageRangeMeters = 20f;
        public const float PartyDamageWindowSeconds = 15f;
        public const float MidFightRangeMeters = 20f;
        public static readonly TimeSpan MidFightReviveLockout = TimeSpan.FromMinutes(5);

        public const string HostilePlayerNearbyMessage = "You can't revive while a hostile player is within 20m.";
        public const string PartyRecentlyDamagedMessage = "After a player kill, you can't revive until your party near the target has gone 15 seconds without taking damage.";
        public const string MidFightLockoutMessage = "That player was revived mid-fight less than 5 minutes ago.";

        private const string KilledByPlayerVariable = "REVIVE_KILLED_BY_PLAYER";
        private const string BeastKilledByPlayerVariable = "REVIVE_BEAST_KILLED_BY_PLAYER_";

        private static readonly Dictionary<string, DateTime> _lastMidFightReviveAt = new();
        private static readonly Dictionary<string, DateTime> _deathAt = new();

        /// <summary>Records when a player died and whether a player, or a player's pet or droid, dealt the killing blow.</summary>
        public static void RecordPlayerDeath(uint deceased, uint killer)
        {
            SetLocalBool(deceased, KilledByPlayerVariable, PvpCombat.IsPlayerControlled(killer));
            _deathAt[GetObjectUUID(deceased)] = DateTime.UtcNow;
        }

        /// <summary>Records on the master whether this beast was killed by a player or a player's pet.</summary>
        public static void RecordBeastDeath(uint master, string beastId, uint killer)
        {
            if (!GetIsObjectValid(master) || string.IsNullOrWhiteSpace(beastId))
                return;

            SetLocalBool(master, BeastKilledByPlayerVariable + beastId, PvpCombat.IsPlayerControlled(killer));
            _deathAt[BeastDeathKey(master, beastId)] = DateTime.UtcNow;
        }

        public static string ValidateResuscitation(uint caster, uint body)
        {
            var pvpDenial = ValidatePvp(caster, body, GetLocalBool(body, KilledByPlayerVariable), GetObjectUUID(body));
            if (!string.IsNullOrEmpty(pvpDenial))
                return pvpDenial;

            if (PvpCombat.IsPlayer(body) &&
                IsMidFight(caster, body) &&
                IsWithinMidFightLockout(GetLastMidFightRevive(body), DateTime.UtcNow))
            {
                return MidFightLockoutMessage;
            }

            return string.Empty;
        }

        /// <summary>The dead beast has no body, so every check measures from the caster.</summary>
        public static string ValidateReviveBeast(uint caster, string beastId)
        {
            return ValidatePvp(caster, caster, GetLocalBool(caster, BeastKilledByPlayerVariable + beastId), BeastDeathKey(caster, beastId));
        }

        /// <summary>Starts the 5-minute mid-fight lockout when a player is revived while a fight is still going.</summary>
        public static void RecordResuscitation(uint caster, uint body)
        {
            if (PvpCombat.IsPlayer(body) && IsMidFight(caster, body))
                _lastMidFightReviveAt[GetObjectUUID(body)] = DateTime.UtcNow;
        }

        public static bool IsWithinMidFightLockout(DateTime? lastMidFightReviveAt, DateTime now)
        {
            return lastMidFightReviveAt.HasValue && now - lastMidFightReviveAt.Value < MidFightReviveLockout;
        }

        /// <summary>A killing blow counts as party damage, so a recent death alone holds revives for 15 seconds.</summary>
        public static bool IsWithinPartyDamageWindow(DateTime? diedAt, DateTime now)
        {
            return diedAt.HasValue && (now - diedAt.Value).TotalSeconds < PartyDamageWindowSeconds;
        }

        private static string BeastDeathKey(uint master, string beastId) => GetObjectUUID(master) + ":" + beastId;

        private static DateTime? GetLastMidFightRevive(uint body)
        {
            return _lastMidFightReviveAt.TryGetValue(GetObjectUUID(body), out var revivedAt) ? revivedAt : null;
        }

        private static string ValidatePvp(uint caster, uint body, bool killedByPlayer, string deathKey)
        {
            if (IsHostilePlayerNear(caster, caster) || body != caster && IsHostilePlayerNear(caster, body))
                return HostilePlayerNearbyMessage;

            if (killedByPlayer &&
                (IsWithinPartyDamageWindow(GetDeathTime(deathKey), DateTime.UtcNow) || HasPartyTakenRecentDamageNear(caster, body)))
                return PartyRecentlyDamagedMessage;

            return string.Empty;
        }

        private static bool IsHostilePlayerNear(uint caster, uint center)
        {
            // Players come back nearest first, so the walk ends at the first one outside the range.
            for (var nth = 1; ; nth++)
            {
                var player = GetNearestCreature(CreatureType.PlayerCharacter, 1, center, nth);
                if (!GetIsObjectValid(player) || GetDistanceBetween(center, player) > HostilePlayerRangeMeters)
                    return false;

                if (player == caster || GetIsDM(player) || GetIsDMPossessed(player) || GetIsDead(player))
                    continue;

                if (GetIsReactionTypeHostile(player, caster) || GetIsReactionTypeHostile(caster, player))
                    return true;
            }
        }

        private static DateTime? GetDeathTime(string key)
        {
            return _deathAt.TryGetValue(key, out var diedAt) ? diedAt : null;
        }

        private static bool HasPartyTakenRecentDamageNear(uint caster, uint body)
        {
            foreach (var member in Party.GetAllPartyMembers(caster))
            {
                if (!GetIsObjectValid(member) ||
                    GetArea(member) != GetArea(body) ||
                    member != body && GetDistanceBetween(member, body) > PartyDamageRangeMeters)
                {
                    continue;
                }

                // The dead never record their killing blow as recent damage, so the time they died stands in for it.
                var recentlyHit = GetIsDead(member)
                    ? IsWithinPartyDamageWindow(GetDeathTime(GetObjectUUID(member)), DateTime.UtcNow)
                    : Combat.HasRecentDamageTaken(member, PartyDamageWindowSeconds);
                if (recentlyHit)
                    return true;
            }

            return false;
        }

        /// <summary>A fight is on when the caster or a nearby party member is in combat, or a living enemy is within 20m.</summary>
        private static bool IsMidFight(uint caster, uint body)
        {
            if (GetIsInCombat(caster) || IsLivingEnemyNear(caster) || IsLivingEnemyNear(body))
                return true;

            foreach (var member in Party.GetAllPartyMembersWithinRange(caster, MidFightRangeMeters))
            {
                if (GetIsInCombat(member))
                    return true;
            }

            return false;
        }

        private static bool IsLivingEnemyNear(uint center)
        {
            var enemy = GetNearestCreature(
                CreatureType.Reputation,
                (int)ReputationType.Enemy,
                center,
                1,
                (int)CreatureType.IsAlive,
                1);
            return GetIsObjectValid(enemy) && GetDistanceBetween(center, enemy) <= MidFightRangeMeters;
        }
    }
}
