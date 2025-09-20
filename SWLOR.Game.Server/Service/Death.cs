
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Event;
using SWLOR.Shared.Core.Log;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Core.Service;

namespace SWLOR.Game.Server.Service
{
    public class Death
    {
        private static ILogger _logger = ServiceContainer.GetService<ILogger>();
        private static readonly IDatabaseService _db = ServiceContainer.GetService<IDatabaseService>();
        /// <summary>
        /// When a player starts dying, instantly kill them.
        /// </summary>
        [ScriptHandler(ScriptName.OnModuleDying)]
        public static void OnPlayerDying()
        {
            ApplyEffectToObject(DurationType.Instant, EffectDeath(), GetLastPlayerDying());
        }

        /// <summary>
        /// Handles resetting a player's standard faction reputations and displaying the respawn pop-up menu.
        /// </summary>
        [ScriptHandler(ScriptName.OnModuleDeath)]
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

            if (GetIsPC(hostile) && !GetIsDM(hostile) && !GetIsDMPossessed(hostile))
            {
                var hostilePlayerId = GetObjectUUID(hostile);
                var dbHostilePlayer = _db.Get<Player>(hostilePlayerId);
                if (dbHostilePlayer != null && dbHostilePlayer.Settings.IsSubdualModeEnabled)
                {
                    SendMessageToPC(player, "You have been subdued.");
                    Messaging.SendMessageNearbyToPlayers(player, $"{GetName(player)} has been subdued by {GetName(hostile)}.");
                    ApplyEffectToObject(DurationType.Instant, EffectResurrection(), player);
                    ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), player, 60f);
                    ApplyEffectToObject(DurationType.Temporary, EffectSlow(), player, 300f);
                    ApplyEffectToObject(DurationType.Temporary, EffectACDecrease(10), player, 300f);
                    ApplyEffectToObject(DurationType.Temporary, EffectAccuracyDecrease(10), player, 300f);
                }
            }
            else
            {
                const string RespawnMessage = "You have died. Wait for another player to revive you or respawn to go to your registered medical center.";
                PopUpDeathGUIPanel(player, true, true, 0, RespawnMessage);

                WriteAudit(player);
            }
        }

        /// <summary>
        /// Handles setting player's HP, FP, and STM to half of maximum,
        /// applies penalties for death, and teleports him or her to their home point.
        /// </summary>
        [ScriptHandler(ScriptName.OnModuleRespawn)]
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
        [ScriptHandler(ScriptName.OnModuleEnter)]
        public static void InitializeRespawnPoint()
        {
            var player = GetEnteringObject();

            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId) ?? new Player(playerId);

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

            _db.Set(dbPlayer);
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
            _logger.Write<DeathLogGroup>(log);
        }


        /// <summary>
        /// Teleports player to his or her last home point.
        /// </summary>
        /// <param name="player">The player to teleport</param>
        private static void SendToHomePoint(uint player)
        {
            var playerId = GetObjectUUID(player);
            var entity = _db.Get<Player>(playerId);
            var area = Area.GetAreaByResref(entity.RespawnAreaResref);
            var position = Vector3(
                entity.RespawnLocationX,
                entity.RespawnLocationY,
                entity.RespawnLocationZ);

            if (!GetIsObjectValid(area))
            {
                var defaultLocation = GetLocation(GetWaypointByTag("DTH_DEFAULT_RESPAWN_POINT"));
                AssignCommand(player, () => ActionJumpToLocation(defaultLocation));
            }
            else
            {
                var location = Location(area, position, entity.RespawnLocationOrientation);
                AssignCommand(player, () => ActionJumpToLocation(location));
            }
        }

        /// <summary>
        /// Applies death penalties for a player.
        /// </summary>
        /// <param name="player">The player who we're applying penalties to</param>
        private static int ApplyPenalties(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
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

            var social = GetAbilityScore(player, AbilityType.Social);
            var newDebt = dbPlayer.TotalSPAcquired * multiplier;
            var reductionBonus = 0f;
            reductionBonus += Property.GetEffectiveUpgradeLevel(dbPlayer.CitizenPropertyId, PropertyUpgradeType.MedicalCenterLevel) * 0.05f; // -5% per Medical Center level

            if (social > 10)
            {
                reductionBonus += (social - 10) * 0.03f; // -3% per SOC
            }

            if (reductionBonus > 0.8f)
                reductionBonus = 0.8f;

            newDebt -= (int)(newDebt * reductionBonus);

            dbPlayer.XPDebt += newDebt;

            const int MaxDebt = 9999999;
            if (dbPlayer.XPDebt > MaxDebt)
                dbPlayer.XPDebt = MaxDebt;

            _db.Set(dbPlayer);

            SendMessageToPC(player, $"{newDebt} XP added to your debt. (Total: {dbPlayer.XPDebt} XP)");

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

            _logger.Write<DeathLogGroup>(log);
        }
    }
}
