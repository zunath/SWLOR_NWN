using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using NWNXLib = NWN.Native.API.NWNXLib;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// Replaces the base game's stealth detection with a single opposed check per observer/target
    /// pair. The engine re-queries spot detection up to five times per second when modifiers change,
    /// so verdicts are cached and re-rolled at most once per pair per detection window. Listen
    /// detection is suppressed entirely; Detection vs Stealth is the only stat pair.
    /// </summary>
    public class StealthService : IStealthService
    {
        public const string CombatEntryWindowVariable = "STEALTH_COMBAT_ENTRY_WINDOW";

        private const string TimedExitActiveVariable = "STEALTH_TIMED_EXIT_ACTIVE";
        private const string GrantedStealthVariable = "STEALTH_GRANTED_WINDOW";
        private const string GrantedStealthBonusVariable = "STEALTH_GRANTED_BONUS";
        private const float DetectionCheckIntervalSeconds = 30f;
        private const float CloseRangeDetectionCheckIntervalSeconds = 6f;
        private const float CloseRangeDetectionDistance = 5f;
        private const int CloseRangeDetectionBonus = 10;
        private const int CachePruneThreshold = 2000;

        private readonly Dictionary<(uint Observer, uint Target), (bool Detected, DateTime RolledAt)> _verdicts = new();

        // Server-wide so a timer outliving its creature never matches a window on a reused object ID.
        private int _lastTimedExitGeneration;

        private readonly Lazy<IPerkService> _perk;
        private readonly Lazy<IStatService> _stat;
        private readonly Lazy<IStatusEffectService> _statusEffect;
        private readonly Lazy<IEnmityService> _enmity;
        private readonly IAIService _ai;
        private readonly Lazy<IEspionageInfiltrationService> _espionageInfiltration;

        public StealthService(
            Lazy<IPerkService> perk,
            Lazy<IStatService> stat,
            Lazy<IStatusEffectService> statusEffect,
            Lazy<IEnmityService> enmity,
            IAIService ai,
            Lazy<IEspionageInfiltrationService> espionageInfiltration)
        {
            _perk = perk;
            _stat = stat;
            _statusEffect = statusEffect;
            _enmity = enmity;
            _ai = ai;
            _espionageInfiltration = espionageInfiltration;
        }

        /// <summary>
        /// Baseline stealth is only usable by characters with the Stealth perk and only out of
        /// combat. NPCs pass through so spawn tables can still field stealthy creatures.
        /// </summary>
        [NWNEventHandler(ScriptName.OnStealthEnterBefore)]
        public void GateStealthEntry()
        {
            var creature = OBJECT_SELF;

            if (!GetIsPC(creature) || GetIsDM(creature))
                return;

            if (!CanUseStealth(creature))
            {
                EventsPlugin.SkipEvent();
                SendMessageToPC(creature, ColorToken.Red("You have not learned to move unseen. The Stealth perk is required."));
                return;
            }

            if (GetIsInCombat(creature) && GetLocalInt(creature, CombatEntryWindowVariable) == 0)
            {
                EventsPlugin.SkipEvent();
                SendMessageToPC(creature, ColorToken.Red("You cannot enter stealth while in combat."));
            }
        }

        [NWNEventHandler(ScriptName.OnStealthEnterAfter)]
        public void OnStealthEntered()
        {
            var creature = OBJECT_SELF;

            if (!GetIsPC(creature) || GetIsDM(creature))
                return;

            // NWNX always fires the AFTER event even when the BEFORE event rejected the engine
            // transition. Do not create a status icon or start stamina drain unless stealth really
            // became active and the player owns the perk.
            var ownsStealth = CanUseStealth(creature);
            var enteredDuringCombatWithoutWindow =
                GetIsInCombat(creature) &&
                GetLocalInt(creature, CombatEntryWindowVariable) == 0;
            if (!GetActionMode(creature, ActionMode.Stealth) ||
                !ownsStealth ||
                enteredDuringCombatWithoutWindow)
            {
                if (GetActionMode(creature, ActionMode.Stealth))
                {
                    AssignCommand(creature, () =>
                    {
                        SetActionMode(creature, ActionMode.Stealth, false);
                    });
                }

                DeleteLocalInt(creature, TimedExitActiveVariable);
                ClearGrantedStealth(creature);
                RemoveStealthStatus(creature);
                return;
            }

            // Native stealth does not interrupt our custom rest effect or its looping animation.
            if (HasRestStatus(creature))
            {
                RemoveRestStatus(creature);
                AssignCommand(creature, () => ClearAllActions());
            }

            ClearVerdictsForTarget(creature);
            ApplyStealthStatus(creature);
            SilenceLightsaberWhileHidden(creature);

            // Enemies already fighting the player (Ghost Protocol) must roll detection now and
            // drop the player as a target if they fail, instead of swinging until their next round.
            // Deferred so a successful detection does not exit stealth inside this event.
            DelayCommand(0f, () =>
            {
                if (GetIsObjectValid(creature) && GetActionMode(creature, ActionMode.Stealth))
                    ReevaluateEnemyAttackTargets(creature);
            });
        }

        /// <summary>
        /// Rebuilds the active Stealth status after a rank purchase or refund so the status never
        /// retains a stat snapshot from the previous perk level. A full refund also exits the
        /// native action mode immediately because the ownership gate no longer passes.
        /// </summary>
        public void RefreshActiveStatusAfterPerkLevelChange(uint creature)
        {
            if (!GetIsPC(creature) || GetIsDM(creature))
                return;

            var hadActiveStatus = HasStealthStatus(creature);
            RemoveStealthStatus(creature);
            if (hadActiveStatus)
            {
                WriteStructuredLog(
                    LogGroup.AI,
                    "Stealth perk refresh removed active status snapshot: Creature={Creature}",
                    creature);
            }

            if (!GetActionMode(creature, ActionMode.Stealth))
                return;

            if (!CanUseStealth(creature))
            {
                WriteStructuredLog(
                    LogGroup.AI,
                    "Stealth perk full refund forced native stealth exit: Creature={Creature}",
                    creature);
                CancelInfiltration(creature);
                ClearVerdictsForTarget(creature);
                AssignCommand(creature, () =>
                {
                    SetActionMode(creature, ActionMode.Stealth, false);
                });
                return;
            }

            ClearVerdictsForTarget(creature);
            ApplyStealthStatus(creature);
            WriteStructuredLog(
                LogGroup.AI,
                "Stealth perk refresh reapplied active status snapshot: Creature={Creature}",
                creature);
        }

        /// <summary>
        /// The Stealth status carries the stamina drain, so a stealthed player must never be
        /// without it. Rebuilds the status if something removed it while native stealth stayed on.
        /// </summary>
        [NWNEventHandler(ScriptName.OnPlayerHeartbeat)]
        public void EnsureStealthStatusWhileHidden()
        {
            var creature = OBJECT_SELF;

            if (!GetIsPC(creature) ||
                GetIsDM(creature) ||
                !GetActionMode(creature, ActionMode.Stealth) ||
                HasStealthStatus(creature))
                return;

            WriteStructuredLog(
                LogGroup.AI,
                "Stealth status missing during native stealth, refreshing: Creature={Creature}",
                creature);
            RefreshActiveStatusAfterPerkLevelChange(creature);
        }

        /// <summary>
        /// Records player-initiated combat before the attack event can add pair enmity. This lets
        /// infiltration distinguish the attack from combat caused by a successful detection.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureAttackBefore)]
        public void RecordPlayerCombatInitiation()
        {
            var enemy = OBJECT_SELF;
            var attacker = GetLastAttacker(enemy);

            if (!GetIsPC(attacker) || GetIsDM(attacker))
                return;

            RecordInfiltrationCombatInitiation(attacker);
        }

        /// <summary>
        /// Landing a hit is a hostile action, so it reveals the attacker. Abilities flagged
        /// BreaksStealth are already handled on activation; this covers auto-attacks and any
        /// damage-dealing path that does not route through an ability.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSWLORDamage)]
        public void BreakStealthOnDamageDealt()
        {
            var attacker = OBJECT_SELF;

            if (!GetIsPC(attacker) ||
                GetIsDM(attacker) ||
                !GetActionMode(attacker, ActionMode.Stealth))
                return;

            AssignCommand(attacker, () =>
            {
                SetActionMode(attacker, ActionMode.Stealth, false);
            });
            SendMessageToPC(attacker, "Your attack gives away your position.");
        }

        /// <summary>
        /// Ends the stealth an ability grants after <paramref name="durationSeconds"/>. Leaving
        /// stealth first releases the window, so a later stealth entry is never ended by this timer.
        /// </summary>
        public int ScheduleTimedExit(uint creature, float durationSeconds)
        {
            var generation = ++_lastTimedExitGeneration;
            SetLocalInt(creature, TimedExitActiveVariable, generation);

            DelayCommand(durationSeconds, () =>
            {
                if (!GetIsObjectValid(creature))
                    return;

                // A grant whose entry never happened must not outlive its window either.
                if (GetLocalInt(creature, GrantedStealthVariable) == generation)
                    ClearGrantedStealth(creature);

                if (GetLocalInt(creature, TimedExitActiveVariable) != generation)
                    return;

                DeleteLocalInt(creature, TimedExitActiveVariable);
                if (!GetActionMode(creature, ActionMode.Stealth))
                    return;

                AssignCommand(creature, () =>
                {
                    SetActionMode(creature, ActionMode.Stealth, false);
                });
            });

            return generation;
        }

        /// <summary>
        /// Lets a player without the Stealth perk enter stealth for <paramref name="durationSeconds"/>
        /// (Shared Presence). The granted stealth uses <paramref name="stealthBonus"/> in place of the
        /// player's own perk bonus, drains no stamina, earns no bypass XP, and ends on the timer, on
        /// detection or hostile action, or when the player leaves stealth. Returns false when the
        /// player cannot be hidden now (not a player, dead, in combat, or already in stealth).
        /// </summary>
        public bool TryGrantStealth(uint creature, int stealthBonus, float durationSeconds)
        {
            if (!CanReceiveGrantedStealth(
                    GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature),
                    GetIsDead(creature) || GetCurrentHitPoints(creature) <= 0,
                    GetIsInCombat(creature),
                    GetActionMode(creature, ActionMode.Stealth)))
                return false;

            // The timer runs on the member so it survives the caster logging out or leaving.
            AssignCommand(creature, () =>
            {
                var generation = ScheduleTimedExit(creature, durationSeconds);
                SetLocalInt(creature, GrantedStealthVariable, generation);
                SetLocalInt(creature, GrantedStealthBonusVariable, Math.Max(0, stealthBonus));
                SetActionMode(creature, ActionMode.Stealth, true);
            });

            return true;
        }

        /// <summary>
        /// Only a living player out of combat who is not already hidden can be granted stealth.
        /// A player already in stealth keeps their own stealth and its rules.
        /// </summary>
        public bool CanReceiveGrantedStealth(bool isPlayer, bool isDead, bool isInCombat, bool isInStealth)
        {
            return isPlayer && !isDead && !isInCombat && !isInStealth;
        }

        /// <summary>
        /// Returns whether the creature's current stealth window was granted by another player.
        /// </summary>
        public bool HasGrantedStealth(uint creature)
        {
            return GetLocalInt(creature, GrantedStealthVariable) != 0;
        }

        /// <summary>
        /// The granting caster's Stealth perk bonus, used in place of the member's own.
        /// </summary>
        public int GetGrantedStealthBonus(uint creature)
        {
            return GetLocalInt(creature, GrantedStealthBonusVariable);
        }

        private void ClearGrantedStealth(uint creature)
        {
            DeleteLocalInt(creature, GrantedStealthVariable);
            DeleteLocalInt(creature, GrantedStealthBonusVariable);
        }

        private bool CanUseStealth(uint creature)
        {
            return GetStealthPerkLevel(creature) > 0 || HasGrantedStealth(creature);
        }

        /// <summary>
        /// Local variables are saved with the character, but the window's timer belongs to the old
        /// creature object, so a grant never outlives the session that received it.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public void ClearGrantedStealthOnEnter()
        {
            ClearGrantedStealth(GetEnteringObject());
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public void ClearGrantedStealthOnExit()
        {
            ClearGrantedStealth(GetExitingObject());
        }

        /// <summary>
        /// A granted window ends with the player's death so a quick respawn cannot reuse it.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public void ClearGrantedStealthOnDeath()
        {
            ClearGrantedStealth(GetLastPlayerDied());
        }

        [NWNEventHandler(ScriptName.OnStealthExitAfter)]
        public void OnStealthExited()
        {
            var creature = OBJECT_SELF;

            DeleteLocalInt(creature, TimedExitActiveVariable);
            ClearGrantedStealth(creature);
            CancelInfiltration(creature);
            RemoveStealthStatus(creature);
            ClearVerdictsForTarget(creature);

            if (!GetIsPC(creature) || GetIsDM(creature))
                return;

            RestoreLightsaberAfterHidden(creature);

            // Enemies that lost the player while hidden hold without attacking; resume them now
            // instead of waiting for their next AI event. Enemies the player walked up to while
            // hidden have no enmity to resume, and their aggro aura only checks on entry, so they
            // notice the player here. Deferred because stealth can end inside the native Spot check.
            DelayCommand(0f, () =>
            {
                if (!GetIsObjectValid(creature) || GetActionMode(creature, ActionMode.Stealth))
                    return;

                ReevaluateEnemyAttackTargets(creature);
                AcquireAggroFromNearbyEnemies(creature);
            });
        }

        /// <summary>
        /// The engine only raises this event when the target is stealthed (invisibility is resolved
        /// before the event fires), so every call is a real stealth-vs-detection question.
        /// </summary>
        [NWNEventHandler(ScriptName.OnDoSpotDetectionBefore)]
        public void ResolveSpotDetection()
        {
            var observer = OBJECT_SELF;
            var target = StringToObject(EventsPlugin.GetEventData("TARGET"));

            EventsPlugin.SkipEvent();

            if (!GetIsObjectValid(target))
            {
                EventsPlugin.SetEventResult("0");
                return;
            }

            var detected = ResolveDetection(observer, target, true);
            EventsPlugin.SetEventResult(detected ? "1" : "0");
        }

        /// <summary>
        /// Prevents the custom hostile aggro aura from bypassing native stealth visibility. The
        /// aura itself is positional and fires even when the hostile has not detected the player.
        /// </summary>
        public bool CanAcquireAggro(uint observer, uint target)
        {
            return !IsHiddenByStealth(observer, target);
        }

        /// <summary>
        /// Returns whether the observer cannot see the target, either through invisibility or
        /// because the target is a stealthed player the observer has not detected.
        /// </summary>
        public bool IsHiddenFrom(uint observer, uint target)
        {
            return IsHiddenByInvisibility(observer, target) ||
                   IsHiddenByStealth(observer, target);
        }

        /// <summary>
        /// Returns whether a stealthed player is hidden from the observer. An observer that could
        /// not make a native Spot check (out of sight range or no line of sight) cannot find the
        /// player; otherwise this uses the same cached Detection vs Stealth verdict as spot
        /// detection, so a successful hostile detection still reveals the player.
        /// </summary>
        public bool IsHiddenByStealth(uint observer, uint target)
        {
            if (!GetIsPC(target) ||
                GetIsDM(target) ||
                !GetActionMode(target, ActionMode.Stealth))
            {
                return false;
            }

            if (!CanAttemptSpotDetection(observer, target))
                return true;

            return !ResolveDetection(observer, target, false);
        }

        private bool CanAttemptSpotDetection(uint observer, uint target)
        {
            return GetArea(observer) == GetArea(target) &&
                   GetDistanceBetween(observer, target) <= GetSpotCheckDistance(observer) &&
                   LineOfSightObject(observer, target);
        }

        /// <summary>
        /// The observer's own Spot range, as the engine's perception update uses it: the creature's
        /// configured sight range, or its longer in-combat range while it is in combat.
        /// </summary>
        protected virtual float GetSpotCheckDistance(uint observer)
        {
            var creature = NWNXLib.g_pAppManager.m_pServerExoApp.GetCreatureByGameObjectID(observer);
            return creature?.GetSpotCheckDistance() ?? 0f;
        }

        /// <summary>
        /// Returns whether the creature is hidden from other players: in stealth or invisible. Signals
        /// that would tell nearby players such a creature is there go only to the creature itself.
        /// </summary>
        public bool IsConcealed(uint creature)
        {
            return GetIsObjectValid(creature) &&
                   (GetActionMode(creature, ActionMode.Stealth) || IsInvisible(creature));
        }

        /// <summary>
        /// Returns whether a creature has an invisibility effect.
        /// </summary>
        public bool IsInvisible(uint creature)
        {
            return GetIsObjectValid(creature) &&
                   HasEffect(creature, EffectTypeScript.Invisibility, EffectTypeScript.ImprovedInvisibility);
        }

        /// <summary>
        /// Returns whether invisibility hides the target from the observer. See Invisibility and
        /// True Seeing let the observer see through it. Listen detection is suppressed, so a hidden
        /// target cannot be located by sound either.
        /// </summary>
        public bool IsHiddenByInvisibility(uint observer, uint target)
        {
            return IsInvisible(target) &&
                   !HasEffect(observer, EffectTypeScript.SeeInvisible, EffectTypeScript.TrueSeeing);
        }

        private bool HasEffect(uint creature, EffectTypeScript effectType, EffectTypeScript otherEffectType)
        {
            if (!MayHaveScriptType(creature, effectType, otherEffectType))
                return false;

            for (var effect = GetFirstEffect(creature); GetIsEffectValid(effect); effect = GetNextEffect(creature))
            {
                var type = GetEffectType(effect);
                if (type == effectType || type == otherEffectType)
                    return true;
            }

            return false;
        }

        [NWNEventHandler(ScriptName.OnDoListenDetectionBefore)]
        public void SuppressListenDetection()
        {
            EventsPlugin.SkipEvent();
            EventsPlugin.SetEventResult("0");
        }

        private bool GetOrRollVerdict(uint observer, uint target)
        {
            var now = UtcNow();
            var key = (observer, target);
            var playerObserver = IsPlayerObserver(observer);
            var closeRange = playerObserver && IsWithinCloseDetectionRange(observer, target);

            // The lifetime is judged on lookup, so an observer who closes to arm's length re-rolls
            // within the short window instead of waiting out a verdict rolled from afar.
            if (_verdicts.TryGetValue(key, out var verdict) &&
                verdict.RolledAt.AddSeconds(GetDetectionCheckIntervalSeconds(closeRange)) > now)
                return verdict.Detected;

            var detectionDie = Random.D20(1);
            var detected = IsStealthDetectionBlocked(observer)
                ? IsCloudedDetectionRollSuccessful(detectionDie, playerObserver)
                : IsDetectionRollSuccessful(
                    detectionDie,
                    GetDetection(observer) + GetCloseRangeDetectionBonus(closeRange),
                    Random.D20(1),
                    GetStealth(target),
                    playerObserver);

            if (_verdicts.Count >= CachePruneThreshold)
                PruneExpired(now);

            _verdicts[key] = (detected, now);
            return detected;
        }

        /// <summary>
        /// Detection beats Stealth on a higher total; ties go to the hidden creature. For a player
        /// observer a natural 20 always detects, so no Stealth lead hides a creature from players
        /// entirely. NPC observers have no such floor, keeping capped NPC Detection unable to
        /// find a fully committed sneak.
        /// </summary>
        public bool IsDetectionRollSuccessful(
            int detectionDie,
            int detection,
            int stealthDie,
            int stealth,
            bool naturalTwentyDetects)
        {
            return (naturalTwentyDetects && detectionDie >= 20) ||
                   detectionDie + detection > stealthDie + stealth;
        }

        /// <summary>
        /// A clouded observer (Cloud Mind) cannot spot anyone in stealth. A clouded player still
        /// spots on a natural 20, so stealth is never absolute against a player.
        /// </summary>
        public bool IsCloudedDetectionRollSuccessful(int detectionDie, bool playerObserver)
        {
            return playerObserver && detectionDie >= 20;
        }

        /// <summary>
        /// Returns whether the observer cannot spot stealth (for example under Cloud Mind).
        /// </summary>
        public bool IsStealthDetectionBlocked(uint observer)
        {
            return GetStealthDetectionBlockedAdjustment(observer) > 0;
        }

        public int GetCloseRangeDetectionBonus(bool closeRange)
        {
            return closeRange ? CloseRangeDetectionBonus : 0;
        }

        public float GetDetectionCheckIntervalSeconds(bool closeRange)
        {
            return closeRange ? CloseRangeDetectionCheckIntervalSeconds : DetectionCheckIntervalSeconds;
        }

        /// <summary>
        /// Player observers get the close-range and natural 20 rules. NPC observers keep the flat
        /// check so infiltration content stays tuned as it is.
        /// </summary>
        private bool IsPlayerObserver(uint observer)
        {
            return GetIsPC(observer) &&
                   !GetIsDM(observer) &&
                   !GetIsDMPossessed(observer);
        }

        /// <summary>
        /// Observers at arm's length notice a stealthed creature more easily and more often.
        /// </summary>
        private bool IsWithinCloseDetectionRange(uint observer, uint target)
        {
            return GetArea(observer) == GetArea(target) &&
                   GetDistanceBetween(observer, target) <= CloseRangeDetectionDistance;
        }

        private bool ResolveDetection(uint observer, uint target, bool acquireAggroOnDetection)
        {
            var detected = GetOrRollVerdict(observer, target);
            RecordInfiltrationDetection(observer, target, detected);

            if (detected)
            {
                // Hostility is judged from the observer's perspective. A friendly or neutral
                // observer still sees the target (the verdict stays true), but only a hostile
                // observer's success ends the target's stealth for everyone.
                var hostile = GetIsEnemy(target, observer);

                if (ShouldBreakStealthOnDetection(true, hostile))
                    ExitDetectedPlayerStealth(observer, target);
                if (acquireAggroOnDetection && hostile)
                    TryAcquireAggroAfterDetection(observer, target);
            }

            return detected;
        }

        /// <summary>
        /// Only a successful detection by a hostile observer globally breaks stealth.
        /// </summary>
        public bool ShouldBreakStealthOnDetection(bool detected, bool observerIsHostile)
        {
            return detected && observerIsHostile;
        }

        /// <summary>
        /// A hostile observer's successful detection reveals a player to everyone by ending their
        /// stealth mode. NPC stealth keeps the engine's observer-specific behavior so creature
        /// encounters are not globally revealed when a single observer succeeds.
        /// </summary>
        private void ExitDetectedPlayerStealth(uint observer, uint target)
        {
            if (!GetIsPC(target) ||
                GetIsDM(target) ||
                !GetActionMode(target, ActionMode.Stealth))
                return;

            WriteStructuredLog(
                LogGroup.AI,
                "Stealth detection forced native stealth exit: Observer={Observer} Target={Target}",
                observer,
                target);

            // Set the mode directly while the Spot hook is active, then retry after the native
            // detection call has unwound. Hostile AI can immediately start combat from a
            // successful verdict, and the deferred pass prevents that transition from leaving
            // the player's native mode and tracked status out of sync.
            SetActionMode(target, ActionMode.Stealth, false);
            RemoveStealthStatus(target);
            DelayCommand(0f, () =>
            {
                if (!GetIsObjectValid(target) || !GetActionMode(target, ActionMode.Stealth))
                    return;

                SetActionMode(target, ActionMode.Stealth, false);
                RemoveStealthStatus(target);
            });
            SendMessageToPC(target, ColorToken.Red("You have been detected and are forced out of stealth."));
        }

        private void ClearVerdictsForTarget(uint target)
        {
            var stale = _verdicts.Keys.Where(k => k.Target == target).ToList();
            foreach (var key in stale)
            {
                _verdicts.Remove(key);
            }
        }

        /// <summary>
        /// Drops every cached verdict the observer rolled, so a change to how the observer sees
        /// (Cloud Mind starting or ending) applies on its next check instead of after the cache expires.
        /// </summary>
        public void ClearVerdictsForObserver(uint observer)
        {
            var stale = _verdicts.Keys.Where(k => k.Observer == observer).ToList();
            foreach (var key in stale)
            {
                _verdicts.Remove(key);
            }
        }

        /// <summary>
        /// An NPC observer that just lost sight of stealthed targets stops attacking them now
        /// instead of on its next combat round. Deferred out of the status application.
        /// </summary>
        public void ReevaluateHiddenTargetsForObserver(uint observer)
        {
            if (GetIsPC(observer))
                return;

            DelayCommand(0f, () =>
            {
                if (GetIsObjectValid(observer) && GetEnmityTableCount(observer) > 0)
                    AttackHighestEnmityTarget(observer);
            });
        }

        private void PruneExpired(DateTime now)
        {
            // The longest lifetime bounds every verdict, whatever range it was rolled at.
            var expired = _verdicts
                .Where(kvp => kvp.Value.RolledAt.AddSeconds(DetectionCheckIntervalSeconds) <= now)
                .Select(kvp => kvp.Key)
                .ToList();
            foreach (var key in expired)
            {
                _verdicts.Remove(key);
            }
        }

        protected virtual DateTime UtcNow() => DateTime.UtcNow;

        protected virtual void WriteStructuredLog(LogGroup group, string messageTemplate, params object[] propertyValues) =>
            Log.WriteStructured(group, messageTemplate, propertyValues);

        protected virtual int GetStealthPerkLevel(uint creature) =>
            _perk.Value.GetPerkLevel(creature, PerkType.Stealth);

        protected virtual int GetDetection(uint creature) =>
            _stat.Value.GetDetection(creature);

        protected virtual int GetStealth(uint creature) =>
            _stat.Value.GetStealth(creature);

        protected virtual int GetStealthDetectionBlockedAdjustment(uint observer) =>
            _stat.Value.GetStatAdjustment(observer, StatType.StealthDetectionBlocked);

        protected virtual void RemoveStealthStatus(uint creature) =>
            _statusEffect.Value.RemoveStatusEffect<StealthStatusEffect>(creature);

        protected virtual bool HasStealthStatus(uint creature) =>
            _statusEffect.Value.HasStatusEffect<StealthStatusEffect>(creature);

        protected virtual bool ApplyStealthStatus(uint creature) =>
            _statusEffect.Value.ApplyStatusEffect<StealthStatusEffect>(creature, creature, 0f);

        protected virtual bool HasRestStatus(uint creature) =>
            _statusEffect.Value.HasStatusEffect<RestStatusEffect>(creature);

        protected virtual void RemoveRestStatus(uint creature) =>
            _statusEffect.Value.RemoveStatusEffect<RestStatusEffect>(creature);

        protected virtual void SilenceLightsaberWhileHidden(uint creature) =>
            LightsaberAudio.SilenceWhileHidden(creature);

        protected virtual void RestoreLightsaberAfterHidden(uint creature) =>
            LightsaberAudio.RestoreAfterHidden(creature);

        protected virtual void ReevaluateEnemyAttackTargets(uint creature) =>
            _enmity.Value.ReevaluateEnemyAttackTargets(creature);

        protected virtual void AcquireAggroFromNearbyEnemies(uint target) =>
            _ai.AcquireAggroFromNearbyEnemies(target);

        protected virtual int GetEnmityTableCount(uint enemy) =>
            _enmity.Value.GetEnmityTable(enemy).Count;

        protected virtual void AttackHighestEnmityTarget(uint creature) =>
            _enmity.Value.AttackHighestEnmityTarget(creature);

        protected virtual void CancelInfiltration(uint creature) =>
            _espionageInfiltration.Value.CancelPlayer(creature);

        protected virtual void RecordInfiltrationCombatInitiation(uint attacker) =>
            _espionageInfiltration.Value.RecordPlayerCombatInitiation(attacker);

        protected virtual void RecordInfiltrationDetection(uint observer, uint target, bool detected) =>
            _espionageInfiltration.Value.RecordDetection(observer, target, detected);

        protected virtual void TryAcquireAggroAfterDetection(uint observer, uint target) =>
            _ai.TryAcquireAggroAfterDetection(observer, target);

        protected virtual bool MayHaveScriptType(uint creature, EffectTypeScript effectType, EffectTypeScript otherEffectType) =>
            AppliedEffects.MayHaveScriptType(creature, effectType, otherEffectType);
    }
}
