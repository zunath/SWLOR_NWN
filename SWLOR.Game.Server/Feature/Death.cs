using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature
{
    public class Death
    {
        /// <summary>
        /// Handles resetting a player's standard faction reputations and displaying the respawn pop-up menu.
        /// </summary>
        [NWNEventHandler("mod_death")]
        public static void OnPlayerDeath()
        {

            var player = GetLastPlayerDied();
            var hostile = GetLastHostileActor(player);

            SetStandardFactionReputation(StandardFaction.Commoner, 100, player);
            SetStandardFactionReputation(StandardFaction.Merchant, 100, player);
            SetStandardFactionReputation(StandardFaction.Defender, 100, player);

            var factionMember = GetFirstFactionMember(hostile, false);
            while (GetIsObjectValid(factionMember))
            {
                ClearPersonalReputation(player, factionMember);
                factionMember = GetNextFactionMember(hostile, false);
            }

            const string RespawnMessage = "You have died. You can wait for another player to revive you or respawn to go to your home point.";
            PopUpDeathGUIPanel(player, true, true, 0, RespawnMessage);

            WriteAudit(player);
        }

        /// <summary>
        /// Handles setting player's HP, FP, and STM to half of maximum,
        /// applies penalties for death, and teleports him or her to their home point.
        /// </summary>
        [NWNEventHandler("mod_respawn")]
        public static void OnPlayerRespawn()
        {
            var player = GetLastRespawnButtonPresser();
            var maxHP = GetMaxHitPoints(player);

            var amount = maxHP / 2;
            ApplyEffectToObject(DurationType.Instant, EffectResurrection(), player);
            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), player);

            SendToHomePoint(player);
            var xpLost = ApplyPenalties(player);

            WriteAudit(player, xpLost);
        }

        /// <summary>
        /// Handles setting a player's respawn point if they don't have one set already.
        /// </summary>
        [NWNEventHandler("mod_enter")]
        public static void InitializeRespawnPoint()
        {
            var player = GetEnteringObject();

            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId) ?? new Player();

            // Already have a respawn point, no need to set the default one.
            if (!string.IsNullOrWhiteSpace(dbPlayer.RespawnAreaResref)) return;

            var waypoint = GetWaypointByTag("DEATH_DEFAULT_RESPAWN_POINT");
            var position = GetPosition(waypoint);
            var areaResref = GetResRef(GetArea(waypoint));
            var facing = GetFacing(waypoint);

            dbPlayer.RespawnLocationX = position.X;
            dbPlayer.RespawnLocationY = position.Y;
            dbPlayer.RespawnLocationZ = position.Z;
            dbPlayer.RespawnAreaResref = areaResref;
            dbPlayer.RespawnLocationOrientation = facing;

            DB.Set(playerId, dbPlayer);
        }

        /// <summary>
        /// Write an audit entry with details of this death.
        /// </summary>
        /// <param name="player">The player who died</param>
        private static void WriteAudit(uint player)
        {
            var name = GetName(player);
            var area = GetArea(player);
            var areaName = GetName(area);
            var areaTag = GetTag(area);
            var areaResref = GetResRef(area);
            var hostile = GetLastHostileActor(player);
            var hostileName = GetName(hostile);

            var log = $"DEATH: {name} - {areaName} - {areaTag} - {areaResref} Killed by: {hostileName}";
            Log.Write(LogGroup.Death, log);
        }


        /// <summary>
        /// Teleports player to his or her last home point.
        /// </summary>
        /// <param name="player">The player to teleport</param>
        private static void SendToHomePoint(uint player)
        {
            var playerId = GetObjectUUID(player);
            var entity = DB.Get<Player>(playerId);
            var area = Cache.GetAreaByResref(entity.RespawnAreaResref);
            var position = Vector3(
                entity.RespawnLocationX,
                entity.RespawnLocationY,
                entity.RespawnLocationZ);

            var location = Location(area, position, entity.RespawnLocationOrientation);

            AssignCommand(player, () => ActionJumpToLocation(location));
        }

        /// <summary>
        /// Applies death penalties for a player.
        /// </summary>
        /// <param name="player">The player who we're applying penalties to</param>
        private static int ApplyPenalties(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            int multiplier;

            // 300+
            if (dbPlayer.TotalSPAcquired >= 300)
                multiplier = 45;
            // 200 - 299
            else if (dbPlayer.TotalSPAcquired >= 200)
                multiplier = 35;
            // 50 - 199
            else if (dbPlayer.TotalSPAcquired >= 50)
                multiplier = 25;
            // 0 - 49
            else
                multiplier = 15;
            
            dbPlayer.XPDebt = dbPlayer.TotalSPAcquired * multiplier;
            DB.Set(playerId, dbPlayer);

            return dbPlayer.XPDebt;
        }

        /// <summary>
        /// Writes an audit entry to the Death audit group.
        /// </summary>
        /// <param name="player">The player who respawned</param>
        /// <param name="xpLost">The amount of XP lost</param>
        private static void WriteAudit(uint player, int xpLost)
        {
            var name = GetName(player);
            var log = $"RESPAWN - {name} - {xpLost} XP lost";

            Log.Write(LogGroup.Death, log);
        }
    }
}
