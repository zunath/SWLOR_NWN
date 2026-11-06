using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// Player death, respawn, the XP debt penalty and where a player respawns. The logic that used to be static in
    /// <c>Service.Death</c> is moved here unchanged; the facade keeps the event handlers and forwards every call to the
    /// container's instance. Calls into the unconverted <c>Combat</c>, <c>Ability</c>, <c>StatusEffect</c>,
    /// <c>CombatRecorder</c>, <c>Log</c> and the feature code sit behind one-line <c>protected virtual</c> methods whose
    /// bodies are the original calls.
    /// </summary>
    public class DeathService : IDeathService
    {
        private const string DefaultRespawnWaypointTag = "DTH_DEFAULT_RESPAWN_POINT";
        private const string DebtWaivedVariable = "DEATH_DEBT_WAIVED";

        private readonly IDBService _db;
        private readonly INamedAnimationService _namedAnimation;
        private readonly IMessagingService _messaging;
        private readonly IPlayerNameService _playerName;
        private readonly IPropertyService _property;
        private readonly IAreaService _area;

        public DeathService(
            IDBService db,
            INamedAnimationService namedAnimation,
            IMessagingService messaging,
            IPlayerNameService playerName,
            IPropertyService property,
            IAreaService area)
        {
            _db = db;
            _namedAnimation = namedAnimation;
            _messaging = messaging;
            _playerName = playerName;
            _property = property;
            _area = area;
        }

        public void OnPlayerDying()
        {
            var player = GetLastPlayerDying();
            if (TryPreventFatalDamageAndGrantTemporaryHP(player, 0, restoreToOneHP: true))
                return;

            ApplyEffectToObject(DurationType.Instant, EffectDeath(), player);
        }

        public void OnPlayerDeath()
        {
            var player = GetLastPlayerDied();
            _namedAnimation.ClearOnDeath(player);
            ClearQueuedAbility(player);
            var hostile = GetLastHostileActor(player);
            RecordPlayerDeath(player, hostile);

            SetStandardFactionReputation(StandardFaction.Commoner, 100, player);
            SetStandardFactionReputation(StandardFaction.Merchant, 100, player);
            SetStandardFactionReputation(StandardFaction.Defender, 100, player);

            var factionMember = GetFirstFactionMember(hostile, false);
            while (GetIsObjectValid(factionMember))
            {
                ClearPersonalReputation(player, factionMember);
                factionMember = GetNextFactionMember(hostile, false);
            }

            var isSubdual = IsSubdualDeath(hostile);
            // A subdual charges no XP debt, so Requiem is claimed only by a death that can.
            if (isSubdual)
                DeleteLocalInt(player, DebtWaivedVariable);
            else
                RecordDeathDebtWaiver(player);
            ClearStatusEffectsOnDeath(player);

            if (GetIsPC(hostile) && !GetIsDM(hostile) && !GetIsDMPossessed(hostile))
            {
                if (isSubdual)
                {
                    if (IsRecording())
                        RecordSubdual(player, hostile);
                    SendMessageToPC(player, "You have been subdued.");
                    _messaging.SendMessageNearbyToPlayers(
                        player,
                        receiver => $"{_playerName.GetDisplayName(receiver, player)} has been subdued by {_playerName.GetDisplayName(receiver, hostile)}.");
                    ApplyEffectToObject(DurationType.Instant, EffectResurrection(), player);
                    RestoreGhostAfterRevive(player);
                    DelayCommand(0.1f, () => ReapplyAuraEffectsForCreature(player));
                    ApplyStatusEffect(player, player, typeof(KnockdownStatusEffect), 60f);
                    ApplyStatusEffect(player, player, typeof(SubdualPenaltyStatusEffect), 300f);
                }
            }
            else
            {
                const string RespawnMessage = "You have died. Wait for another player to revive you or respawn to go to your registered medical center.";
                PopUpDeathGUIPanel(player, true, true, 0, RespawnMessage);

                WriteAudit(player);
            }
        }

        private bool IsSubdualDeath(uint hostile)
        {
            if (!GetIsPC(hostile) || GetIsDM(hostile) || GetIsDMPossessed(hostile))
                return false;

            var dbHostilePlayer = _db.Get<Player>(GetObjectUUID(hostile));
            return dbHostilePlayer != null && dbHostilePlayer.Settings.IsSubdualModeEnabled;
        }

        /// <summary>
        /// Status effects are cleared when a player dies, so a death-debt waiver is claimed here and
        /// used when the debt would be charged at respawn. Every death replaces the previous claim.
        /// </summary>
        private void RecordDeathDebtWaiver(uint player)
        {
            DeleteLocalInt(player, DebtWaivedVariable);
            foreach (var effect in GetDeathDebtWaiverEffects(player))
            {
                if (!effect.TryConsumeDeathDebtWaiver())
                    continue;

                SetLocalInt(player, DebtWaivedVariable, 1);
                return;
            }
        }

        public void OnPlayerRespawn()
        {
            var player = GetLastRespawnButtonPresser();
            var maxHP = GetMaxHitPoints(player);

            var amount = maxHP / 2;
            ApplyEffectToObject(DurationType.Instant, EffectResurrection(), player);
            RestoreGhostAfterRevive(player);
            if (IsRecording())
                RecordRespawn(player);
            using (BeginRespawnAttribution())
                ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), player);

            SendToHomePoint(player);
            var xpLost = ApplyPenalties(player);

            WriteAudit(player, xpLost);
        }

        public void InitializeRespawnPoint()
        {
            var player = GetEnteringObject();

            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId) ?? new Player(playerId);

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

            _db.Set(dbPlayer);
        }

        /// <summary>
        /// Write an audit entry with details of this death.
        /// </summary>
        /// <param name="player">The player who died</param>
        private void WriteAudit(uint player)
        {
            var name = _playerName.GetAuditName(player);
            var area = GetArea(player);
            var areaName = GetName(area);
            var areaTag = GetTag(area);
            var areaResref = GetResRef(area);
            var hostile = GetLastHostileActor(player);
            var hostileName = _playerName.GetAuditName(hostile);

            var log = $"DEATH: {name} - {areaName} - {areaTag} - {areaResref} Killed by: {hostileName}";
            WriteLog(LogGroup.Death, log);
        }

        /// <summary>
        /// Teleports player to his or her last home point.
        /// </summary>
        /// <param name="player">The player to teleport</param>
        private void SendToHomePoint(uint player)
        {
            var playerId = GetObjectUUID(player);
            var destination = GetRespawnLocation(_db.Get<Player>(playerId));

            // Jump immediately rather than queueing an action: resurrection and the death
            // state clear the action queue, which left players respawning where they died.
            AssignCommand(player, () =>
            {
                ClearAllActions(true);
                JumpToLocation(destination);
            });
        }

        public Location GetRespawnLocation(Player dbPlayer)
        {
            if (HasRespawnArea(dbPlayer))
            {
                var area = _area.GetAreaByResref(dbPlayer.RespawnAreaResref);
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

        public bool HasRespawnArea(Player dbPlayer)
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
        private int ApplyPenalties(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            if (GetLocalInt(player, DebtWaivedVariable) == 1)
            {
                DeleteLocalInt(player, DebtWaivedVariable);
                SendMessageToPC(player, "Requiem spares you any XP debt.");
                return dbPlayer.XPDebt;
            }

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
            reductionBonus += _property.GetEffectiveUpgradeLevel(dbPlayer.CitizenPropertyId, PropertyUpgradeType.MedicalCenterLevel) * 0.05f; // -5% per Medical Center level

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
        private void WriteAudit(uint player, int xpLost)
        {
            var name = _playerName.GetAuditName(player);
            var log = $"RESPAWN - {name} - {xpLost} XP lost";

            WriteLog(LogGroup.Death, log);
        }

        protected virtual bool TryPreventFatalDamageAndGrantTemporaryHP(uint defender, int damage, bool restoreToOneHP) =>
            Service.Combat.TryPreventFatalDamageAndGrantTemporaryHP(defender, damage, restoreToOneHP);

        protected virtual void ClearQueuedAbility(uint player) => Feature.UsePerkFeat.ClearQueuedAbility(player);

        protected virtual void RecordPlayerDeath(uint deceased, uint killer) =>
            Feature.AbilityDefinition.ReviveRules.RecordPlayerDeath(deceased, killer);

        protected virtual void ClearStatusEffectsOnDeath(uint player) => StatusEffect.ClearStatusEffectsOnDeath(player);

        protected virtual List<IDeathDebtWaiverStatusEffect> GetDeathDebtWaiverEffects(uint player) =>
            StatusEffect.GetCreatureStatusEffects(player).GetAllEffects()
                .OfType<IDeathDebtWaiverStatusEffect>().ToList();

        protected virtual bool IsRecording() => CombatRecorder.IsRecording;

        protected virtual void RecordSubdual(uint subdued, uint subduedBy) => CombatRecorder.RecordSubdual(subdued, subduedBy);

        protected virtual void RecordRespawn(uint player) => CombatRecorder.RecordRespawn(player);

        protected virtual IDisposable BeginRespawnAttribution() => CombatRecorder.BeginRespawnAttribution();

        protected virtual void RestoreGhostAfterRevive(uint creature) => Feature.PlayerTemporaryEffects.RestoreGhostAfterRevive(creature);

        protected virtual void ReapplyAuraEffectsForCreature(uint creature) => Ability.ReapplyAuraEffectsForCreature(creature);

        protected virtual void ApplyStatusEffect(uint source, uint creature, Type statusEffectClass, float durationSeconds) =>
            StatusEffect.ApplyStatusEffect(source, creature, statusEffectClass, durationSeconds);

        protected virtual void WriteLog(LogGroup group, string details) => Log.Write(group, details);
    }
}
