using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    public class Death
    {
        private const string DefaultRespawnWaypointTag = "DTH_DEFAULT_RESPAWN_POINT";

        /// <summary>
        /// When a player starts dying, instantly kill them.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleDying)]
        public static void OnPlayerDying()
        {
            var player = GetLastPlayerDying();
            if (Combat.TryPreventFatalDamageAndGrantTemporaryHP(player, 0, restoreToOneHP: true))
                return;

            ApplyEffectToObject(DurationType.Instant, EffectDeath(), player);
        }

        /// <summary>
        /// Handles resetting a player's standard faction reputations and displaying the respawn pop-up menu.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void OnPlayerDeath()
        {
            var player = GetLastPlayerDied();
            NamedAnimation.ClearOnDeath(player);
            Feature.UsePerkFeat.ClearQueuedAbility(player);

            // A destroyed ship ejects its pilot alive in an escape pod, so there is nothing to revive or respawn.
            if (Space.ApplyDeath(player))
                return;

            var hostile = GetLastHostileActor(player);
            Feature.AbilityDefinition.ReviveRules.RecordPlayerDeath(player, hostile);

            SetStandardFactionReputation(StandardFaction.Commoner, 100, player);
            SetStandardFactionReputation(StandardFaction.Merchant, 100, player);
            SetStandardFactionReputation(StandardFaction.Defender, 100, player);

            var factionMember = GetFirstFactionMember(hostile, false);
            while (GetIsObjectValid(factionMember))
            {
                ClearPersonalReputation(player, factionMember);
                factionMember = GetNextFactionMember(hostile, false);
            }

            StatusEffect.ClearStatusEffectsOnDeath(player);

            if (GetIsPC(hostile) && !GetIsDM(hostile) && !GetIsDMPossessed(hostile))
            {
                var hostilePlayerId = GetObjectUUID(hostile);
                var dbHostilePlayer = DB.Get<Player>(hostilePlayerId);
                if (dbHostilePlayer != null && dbHostilePlayer.Settings.IsSubdualModeEnabled)
                {
                    if (CombatRecorder.IsRecording)
                        CombatRecorder.RecordSubdual(player, hostile);
                    SendMessageToPC(player, "You have been subdued.");
                    Messaging.SendMessageNearbyToPlayers(
                        player,
                        receiver => $"{PlayerName.GetDisplayName(receiver, player)} has been subdued by {PlayerName.GetDisplayName(receiver, hostile)}.");
                    ApplyEffectToObject(DurationType.Instant, EffectResurrection(), player);
                    Feature.PlayerTemporaryEffects.RestoreGhostAfterRevive(player);
                    DelayCommand(0.1f, () => Ability.ReapplyAuraEffectsForCreature(player));
                    StatusEffect.ApplyStatusEffect(player, player, typeof(KnockdownStatusEffect), 60f);
                    StatusEffect.ApplyStatusEffect(player, player, typeof(SubdualPenaltyStatusEffect), 300f);
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
        [NWNEventHandler(ScriptName.OnModuleRespawn)]
        public static void OnPlayerRespawn()
        {
            var player = GetLastRespawnButtonPresser();
            var maxHP = GetMaxHitPoints(player);

            var amount = maxHP / 2;
            ApplyEffectToObject(DurationType.Instant, EffectResurrection(), player);
            Feature.PlayerTemporaryEffects.RestoreGhostAfterRevive(player);
            if (CombatRecorder.IsRecording)
                CombatRecorder.RecordRespawn(player);
            using (CombatRecorder.BeginRespawnAttribution())
                ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), player);

            SendToHomePoint(player);
            var xpLost = ApplyPenalties(player);

            WriteAudit(player, xpLost);
        }

        /// <summary>
        /// Handles setting a player's respawn point if they don't have one set already.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void InitializeRespawnPoint()
        {
            var player = GetEnteringObject();

            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId) ?? new Player(playerId);

            // Already have a respawn point, no need to set the default one.
            if (HasRespawnArea(dbPlayer)) return;

            var waypoint = GetWaypointByTag(DefaultRespawnWaypointTag);
            var position = GetPosition(waypoint);
            var areaResref = GetResRef(GetArea(waypoint));
            var facing = GetFacing(waypoint);

            dbPlayer.RespawnLocationX = position.X;
            dbPlayer.RespawnLocationY = position.Y;
            dbPlayer.RespawnLocationZ = position.Z;
            dbPlayer.RespawnAreaResref = areaResref;
            dbPlayer.RespawnLocationOrientation = facing;

            DB.Set(dbPlayer);
        }

        /// <summary>
        /// Write an audit entry with details of this death.
        /// </summary>
        /// <param name="player">The player who died</param>
        private static void WriteAudit(uint player)
        {
            var name = PlayerName.GetAuditName(player);
            var area = GetArea(player);
            var areaName = GetName(area);
            var areaTag = GetTag(area);
            var areaResref = GetResRef(area);
            var hostile = GetLastHostileActor(player);
            var hostileName = PlayerName.GetAuditName(hostile);

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
            var destination = GetRespawnLocation(DB.Get<Player>(playerId));

            // Jump immediately rather than queueing an action: resurrection and the death
            // state clear the action queue, which left players respawning where they died.
            AssignCommand(player, () =>
            {
                ClearAllActions(true);
                JumpToLocation(destination);
            });
        }

        /// <summary>
        /// Returns where a player respawns: their registered medical center, or the default respawn
        /// point (DTH_DEFAULT_RESPAWN_POINT) when no medical center is saved, its area no longer exists,
        /// or the saved point is not on walkable ground. Never returns the place the player died.
        /// </summary>
        /// <param name="dbPlayer">The player's record. May be null.</param>
        /// <returns>The location to respawn at.</returns>
        public static Location GetRespawnLocation(Player dbPlayer)
        {
            if (HasRespawnArea(dbPlayer))
            {
                var area = Area.GetAreaByResref(dbPlayer.RespawnAreaResref);
                if (GetIsObjectValid(area))
                {
                    var position = Vector3(
                        dbPlayer.RespawnLocationX,
                        dbPlayer.RespawnLocationY,
                        dbPlayer.RespawnLocationZ);
                    var location = Location(area, position, dbPlayer.RespawnLocationOrientation);

                    if (IsWalkable(location))
                        return location;
                }
            }

            return GetLocation(GetWaypointByTag(DefaultRespawnWaypointTag));
        }

        /// <summary>
        /// Whether a player record names a respawn area at all.
        /// </summary>
        /// <param name="dbPlayer">The player's record. May be null.</param>
        /// <returns>true if a respawn area resref is saved, false otherwise.</returns>
        public static bool HasRespawnArea(Player dbPlayer)
        {
            return !string.IsNullOrWhiteSpace(dbPlayer?.RespawnAreaResref);
        }

        private static bool IsWalkable(Location location)
        {
            var material = GetSurfaceMaterial(location);
            return material > 0 &&
                   int.TryParse(Get2DAString("surfacemat", "Walk", material), out var walkable) &&
                   walkable == 1;
        }

        /// <summary>
        /// Applies death penalties for a player.
        /// </summary>
        /// <param name="player">The player who we're applying penalties to</param>
        /// <returns>The player's total XP debt after the penalty.</returns>
        public static int ApplyPenalties(uint player)
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

            DB.Set(dbPlayer);

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
            var name = PlayerName.GetAuditName(player);
            var log = $"RESPAWN - {name} - {xpLost} XP lost";

            Log.Write(LogGroup.Death, log);
        }
    }
}
