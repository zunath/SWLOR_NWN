using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// Player dying, death and respawn. The respawn point, the respawn location, the XP debt penalty and the audit lines are in
    /// <see cref="DeathRespawnService"/>. The logic that used to be static in
    /// <c>Service.Death</c> is moved here unchanged; the event handlers run on the container's instance, and static callers
    /// reach it through <c>GameServices.Death</c>. Calls into the unconverted <c>Combat</c>, <c>Ability</c>, <c>StatusEffect</c>,
    /// <c>CombatRecorder</c> and the feature code sit behind one-line <c>protected virtual</c> methods whose
    /// bodies are the original calls.
    /// </summary>
    public class DeathService : IDeathService
    {
        private const string DebtWaivedVariable = DeathRespawnService.DebtWaivedVariable;

        private readonly IDBService _db;
        private readonly INamedAnimationService _namedAnimation;
        private readonly IMessagingService _messaging;
        private readonly IPlayerNameService _playerName;
        private readonly IStatusEffectService _statusEffect;
        private readonly IStatusEffectApplicationService _statusEffectApplication;
        private readonly ICombatRecorderService _combatRecorder;
        private readonly ICombatRecordDamageService _combatRecordDamage;
        private readonly ICreatureRevivedListener[] _revivedListeners;
        private readonly ICombatDamageTakenService _damageTaken;
        private readonly IDeathRespawnService _respawn;

        public DeathService(
            IDBService db,
            INamedAnimationService namedAnimation,
            IMessagingService messaging,
            IPlayerNameService playerName,
            IStatusEffectService statusEffect,
            IStatusEffectApplicationService statusEffectApplication,
            ICombatRecorderService combatRecorder,
            ICombatRecordDamageService combatRecordDamage,
            IEnumerable<ICreatureRevivedListener> revivedListeners,
            ICombatDamageTakenService damageTaken,
            IDeathRespawnService respawn)
        {
            _db = db;
            _namedAnimation = namedAnimation;
            _messaging = messaging;
            _playerName = playerName;
            _statusEffect = statusEffect;
            _statusEffectApplication = statusEffectApplication;
            _combatRecorder = combatRecorder;
            _combatRecordDamage = combatRecordDamage;
            _revivedListeners = revivedListeners.ToArray();
            _damageTaken = damageTaken;
            _respawn = respawn;
        }

        [NWNEventHandler(ScriptName.OnModuleDying)]
        public void OnPlayerDying()
        {
            var player = GetLastPlayerDying();
            if (TryPreventFatalDamageAndGrantTemporaryHP(player, 0, restoreToOneHP: true))
                return;

            ApplyEffectToObject(DurationType.Instant, EffectDeath(), player);
        }

        [NWNEventHandler(ScriptName.OnModuleDeath)]
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

            // Only a subdual revives on the spot. Every other death, a lethal kill by another
            // player included, leaves the player dead with the respawn panel.
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
            else
            {
                const string RespawnMessage = "You have died. Wait for another player to revive you or respawn to go to your registered medical center.";
                PopUpDeathGUIPanel(player, true, true, 0, RespawnMessage);

                _respawn.WriteAudit(player);
            }
        }

        private bool IsSubdualDeath(uint hostile)
        {
            var hostileIsPlayer = GetIsPC(hostile);
            var hostileIsDM = GetIsDM(hostile) || GetIsDMPossessed(hostile);
            var dbHostilePlayer = hostileIsPlayer && !hostileIsDM
                ? _db.Get<Player>(GetObjectUUID(hostile))
                : null;

            return IsSubdualDeath(hostileIsPlayer, hostileIsDM, dbHostilePlayer);
        }

        /// <summary>
        /// Whether a player's death is a subdual: the killer is a player, not a DM, whose subdual
        /// mode is on. A subdued player is revived where they fell; any other death, including a
        /// lethal kill by another player, shows the respawn panel.
        /// </summary>
        /// <param name="hostileIsPlayer">Whether the killer is player-controlled.</param>
        /// <param name="hostileIsDM">Whether the killer is a DM or a DM-possessed creature.</param>
        /// <param name="dbHostilePlayer">The killer's player record. May be null.</param>
        /// <returns>true if the death is a subdual, false if the player stays dead.</returns>
        public static bool IsSubdualDeath(bool hostileIsPlayer, bool hostileIsDM, Player dbHostilePlayer)
        {
            return hostileIsPlayer &&
                   !hostileIsDM &&
                   dbHostilePlayer?.Settings?.IsSubdualModeEnabled == true;
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

        [NWNEventHandler(ScriptName.OnModuleRespawn)]
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

            _respawn.SendToHomePoint(player);
            var xpDebt = _respawn.ApplyPenalties(player);

            _respawn.WriteAudit(player, xpDebt);
        }

        protected virtual bool TryPreventFatalDamageAndGrantTemporaryHP(uint defender, int damage, bool restoreToOneHP) =>
            _damageTaken.TryPreventFatalDamageAndGrantTemporaryHP(defender, damage, restoreToOneHP);

        protected virtual void ClearQueuedAbility(uint player) => Feature.UsePerkFeat.ClearQueuedAbility(player);

        protected virtual void RecordPlayerDeath(uint deceased, uint killer) =>
            Feature.AbilityDefinition.ReviveRules.RecordPlayerDeath(deceased, killer);

        protected virtual void ClearStatusEffectsOnDeath(uint player) => _statusEffect.ClearStatusEffectsOnDeath(player);

        protected virtual List<IDeathDebtWaiverStatusEffect> GetDeathDebtWaiverEffects(uint player) =>
            _statusEffect.GetCreatureStatusEffects(player).GetAllEffects()
                .OfType<IDeathDebtWaiverStatusEffect>().ToList();

        protected virtual bool IsRecording() => _combatRecorder.IsRecording;

        protected virtual void RecordSubdual(uint subdued, uint subduedBy) => _combatRecordDamage.RecordSubdual(subdued, subduedBy);

        protected virtual void RecordRespawn(uint player) => _combatRecordDamage.RecordRespawn(player);

        protected virtual IDisposable BeginRespawnAttribution() => _combatRecorder.BeginRespawnAttribution();

        protected virtual void RestoreGhostAfterRevive(uint creature) => Feature.PlayerTemporaryEffects.RestoreGhostAfterRevive(creature);

        /// <summary>
        /// Tells the services that keep state by area of effect (the ability auras) that the creature was revived in place, as
        /// the call into <c>Ability</c> did before the ability split.
        /// </summary>
        protected virtual void ReapplyAuraEffectsForCreature(uint creature)
        {
            foreach (var listener in _revivedListeners)
                listener.OnCreatureRevived(creature);
        }

        protected virtual void ApplyStatusEffect(uint source, uint creature, Type statusEffectClass, float durationSeconds) =>
            _statusEffectApplication.ApplyStatusEffect(source, creature, statusEffectClass, durationSeconds);

    }
}
