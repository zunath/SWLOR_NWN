using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using NWNXLib = NWN.Native.API.NWNXLib;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Replaces the base game's stealth detection with a single opposed check per observer/target
    /// pair. The engine re-queries spot detection up to five times per second when modifiers change,
    /// so verdicts are cached and re-rolled at most once per pair per detection window. Listen
    /// detection is suppressed entirely; Detection vs Stealth is the only stat pair.
    /// </summary>
    public static class Stealth
    {
        public const string CombatEntryWindowVariable = "STEALTH_COMBAT_ENTRY_WINDOW";

        private const string TimedExitActiveVariable = "STEALTH_TIMED_EXIT_ACTIVE";
        private const float DetectionCheckIntervalSeconds = 30f;
        private const float CloseRangeDetectionCheckIntervalSeconds = 6f;
        private const float CloseRangeDetectionDistance = 5f;
        private const int CloseRangeDetectionBonus = 10;
        private const int CachePruneThreshold = 2000;

        private static readonly Dictionary<(uint Observer, uint Target), (bool Detected, DateTime RolledAt)> _verdicts = new();

        // Server-wide so a timer outliving its creature never matches a window on a reused object ID.
        private static int _lastTimedExitGeneration;

        /// <summary>
        /// Baseline stealth is only usable by characters with the Stealth perk and only out of
        /// combat. NPCs pass through so spawn tables can still field stealthy creatures.
        /// </summary>
        [NWNEventHandler(ScriptName.OnStealthEnterBefore)]
        public static void GateStealthEntry()
        {
            var creature = OBJECT_SELF;

            if (!GetIsPC(creature) || GetIsDM(creature))
                return;

            if (Perk.GetPerkLevel(creature, PerkType.Stealth) <= 0)
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
        public static void OnStealthEntered()
        {
            var creature = OBJECT_SELF;

            if (!GetIsPC(creature) || GetIsDM(creature))
                return;

            // NWNX always fires the AFTER event even when the BEFORE event rejected the engine
            // transition. Do not create a status icon or start stamina drain unless stealth really
            // became active and the player owns the perk.
            var ownsStealth = Perk.GetPerkLevel(creature, PerkType.Stealth) > 0;
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
                StatusEffect.RemoveStatusEffect<StealthStatusEffect>(creature);
                return;
            }

            // Native stealth does not interrupt our custom rest effect or its looping animation.
            if (StatusEffect.HasStatusEffect<RestStatusEffect>(creature))
            {
                StatusEffect.RemoveStatusEffect<RestStatusEffect>(creature);
                AssignCommand(creature, () => ClearAllActions());
            }

            ClearVerdictsForTarget(creature);
            StatusEffect.ApplyStatusEffect<StealthStatusEffect>(creature, creature, 0f);

            // Enemies already fighting the player (Ghost Protocol) must roll detection now and
            // drop the player as a target if they fail, instead of swinging until their next round.
            // Deferred so a successful detection does not exit stealth inside this event.
            DelayCommand(0f, () =>
            {
                if (GetIsObjectValid(creature) && GetActionMode(creature, ActionMode.Stealth))
                    Enmity.ReevaluateEnemyAttackTargets(creature);
            });
        }

        /// <summary>
        /// Rebuilds the active Stealth status after a rank purchase or refund so the status never
        /// retains a stat snapshot from the previous perk level. A full refund also exits the
        /// native action mode immediately because the ownership gate no longer passes.
        /// </summary>
        public static void RefreshActiveStatusAfterPerkLevelChange(uint creature)
        {
            if (!GetIsPC(creature) || GetIsDM(creature))
                return;

            var hadActiveStatus = StatusEffect.HasStatusEffect<StealthStatusEffect>(creature);
            StatusEffect.RemoveStatusEffect<StealthStatusEffect>(creature);
            if (hadActiveStatus)
            {
                Log.WriteStructured(
                    LogGroup.AI,
                    "Stealth perk refresh removed active status snapshot: Creature={Creature}",
                    creature);
            }

            if (!GetActionMode(creature, ActionMode.Stealth))
                return;

            if (Perk.GetPerkLevel(creature, PerkType.Stealth) <= 0)
            {
                Log.WriteStructured(
                    LogGroup.AI,
                    "Stealth perk full refund forced native stealth exit: Creature={Creature}",
                    creature);
                EspionageInfiltration.CancelPlayer(creature);
                ClearVerdictsForTarget(creature);
                AssignCommand(creature, () =>
                {
                    SetActionMode(creature, ActionMode.Stealth, false);
                });
                return;
            }

            ClearVerdictsForTarget(creature);
            StatusEffect.ApplyStatusEffect<StealthStatusEffect>(creature, creature, 0f);
            Log.WriteStructured(
                LogGroup.AI,
                "Stealth perk refresh reapplied active status snapshot: Creature={Creature}",
                creature);
        }

        /// <summary>
        /// The Stealth status carries the stamina drain, so a stealthed player must never be
        /// without it. Rebuilds the status if something removed it while native stealth stayed on.
        /// </summary>
        [NWNEventHandler(ScriptName.OnPlayerHeartbeat)]
        public static void EnsureStealthStatusWhileHidden()
        {
            var creature = OBJECT_SELF;

            if (!GetIsPC(creature) ||
                GetIsDM(creature) ||
                !GetActionMode(creature, ActionMode.Stealth) ||
                StatusEffect.HasStatusEffect<StealthStatusEffect>(creature))
                return;

            Log.WriteStructured(
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
        public static void RecordPlayerCombatInitiation()
        {
            var enemy = OBJECT_SELF;
            var attacker = GetLastAttacker(enemy);

            if (!GetIsPC(attacker) || GetIsDM(attacker))
                return;

            EspionageInfiltration.RecordPlayerCombatInitiation(attacker);
        }

        /// <summary>
        /// Landing a hit is a hostile action, so it reveals the attacker. Abilities flagged
        /// BreaksStealth are already handled on activation; this covers auto-attacks and any
        /// damage-dealing path that does not route through an ability.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSWLORDamage)]
        public static void BreakStealthOnDamageDealt()
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
        public static void ScheduleTimedExit(uint creature, float durationSeconds)
        {
            var generation = ++_lastTimedExitGeneration;
            SetLocalInt(creature, TimedExitActiveVariable, generation);

            DelayCommand(durationSeconds, () =>
            {
                if (!GetIsObjectValid(creature) || GetLocalInt(creature, TimedExitActiveVariable) != generation)
                    return;

                DeleteLocalInt(creature, TimedExitActiveVariable);
                if (!GetActionMode(creature, ActionMode.Stealth))
                    return;

                AssignCommand(creature, () =>
                {
                    SetActionMode(creature, ActionMode.Stealth, false);
                });
            });
        }

        [NWNEventHandler(ScriptName.OnStealthExitAfter)]
        public static void OnStealthExited()
        {
            var creature = OBJECT_SELF;

            DeleteLocalInt(creature, TimedExitActiveVariable);
            EspionageInfiltration.CancelPlayer(creature);
            StatusEffect.RemoveStatusEffect<StealthStatusEffect>(creature);
            ClearVerdictsForTarget(creature);

            if (!GetIsPC(creature) || GetIsDM(creature))
                return;

            // Enemies that lost the player while hidden hold without attacking; resume them now
            // instead of waiting for their next AI event. Deferred because stealth can end inside
            // the native Spot check.
            DelayCommand(0f, () =>
            {
                if (GetIsObjectValid(creature) && !GetActionMode(creature, ActionMode.Stealth))
                    Enmity.ReevaluateEnemyAttackTargets(creature);
            });
        }

        /// <summary>
        /// The engine only raises this event when the target is stealthed (invisibility is resolved
        /// before the event fires), so every call is a real stealth-vs-detection question.
        /// </summary>
        [NWNEventHandler(ScriptName.OnDoSpotDetectionBefore)]
        public static void ResolveSpotDetection()
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
        public static bool CanAcquireAggro(uint observer, uint target)
        {
            return !IsHiddenByStealth(observer, target);
        }

        /// <summary>
        /// Returns whether the observer cannot see the target, either through invisibility or
        /// because the target is a stealthed player the observer has not detected.
        /// </summary>
        public static bool IsHiddenFrom(uint observer, uint target)
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
        public static bool IsHiddenByStealth(uint observer, uint target)
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

        private static bool CanAttemptSpotDetection(uint observer, uint target)
        {
            return GetArea(observer) == GetArea(target) &&
                   GetDistanceBetween(observer, target) <= GetSpotCheckDistance(observer) &&
                   LineOfSightObject(observer, target);
        }

        /// <summary>
        /// The observer's own Spot range, as the engine's perception update uses it: the creature's
        /// configured sight range, or its longer in-combat range while it is in combat.
        /// </summary>
        private static float GetSpotCheckDistance(uint observer)
        {
            var creature = NWNXLib.g_pAppManager.m_pServerExoApp.GetCreatureByGameObjectID(observer);
            return creature?.GetSpotCheckDistance() ?? 0f;
        }

        /// <summary>
        /// Returns whether a creature has an invisibility effect.
        /// </summary>
        public static bool IsInvisible(uint creature)
        {
            return GetIsObjectValid(creature) &&
                   HasEffect(creature, EffectTypeScript.Invisibility, EffectTypeScript.ImprovedInvisibility);
        }

        /// <summary>
        /// Returns whether invisibility hides the target from the observer. See Invisibility and
        /// True Seeing let the observer see through it. Listen detection is suppressed, so a hidden
        /// target cannot be located by sound either.
        /// </summary>
        public static bool IsHiddenByInvisibility(uint observer, uint target)
        {
            return IsInvisible(target) &&
                   !HasEffect(observer, EffectTypeScript.SeeInvisible, EffectTypeScript.TrueSeeing);
        }

        private static bool HasEffect(uint creature, EffectTypeScript effectType, EffectTypeScript otherEffectType)
        {
            if (!AppliedEffects.MayHaveScriptType(creature, effectType, otherEffectType))
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
        public static void SuppressListenDetection()
        {
            EventsPlugin.SkipEvent();
            EventsPlugin.SetEventResult("0");
        }

        private static bool GetOrRollVerdict(uint observer, uint target)
        {
            var now = DateTime.UtcNow;
            var key = (observer, target);
            var playerObserver = IsPlayerObserver(observer);
            var closeRange = playerObserver && IsWithinCloseDetectionRange(observer, target);

            // The lifetime is judged on lookup, so an observer who closes to arm's length re-rolls
            // within the short window instead of waiting out a verdict rolled from afar.
            if (_verdicts.TryGetValue(key, out var verdict) &&
                verdict.RolledAt.AddSeconds(GetDetectionCheckIntervalSeconds(closeRange)) > now)
                return verdict.Detected;

            var detected = IsDetectionRollSuccessful(
                Random.D20(1),
                Stat.GetDetection(observer) + GetCloseRangeDetectionBonus(closeRange),
                Random.D20(1),
                Stat.GetStealth(target),
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
        public static bool IsDetectionRollSuccessful(
            int detectionDie,
            int detection,
            int stealthDie,
            int stealth,
            bool naturalTwentyDetects)
        {
            return (naturalTwentyDetects && detectionDie >= 20) ||
                   detectionDie + detection > stealthDie + stealth;
        }

        public static int GetCloseRangeDetectionBonus(bool closeRange)
        {
            return closeRange ? CloseRangeDetectionBonus : 0;
        }

        public static float GetDetectionCheckIntervalSeconds(bool closeRange)
        {
            return closeRange ? CloseRangeDetectionCheckIntervalSeconds : DetectionCheckIntervalSeconds;
        }

        /// <summary>
        /// Player observers get the close-range and natural 20 rules. NPC observers keep the flat
        /// check so infiltration content stays tuned as it is.
        /// </summary>
        private static bool IsPlayerObserver(uint observer)
        {
            return GetIsPC(observer) &&
                   !GetIsDM(observer) &&
                   !GetIsDMPossessed(observer);
        }

        /// <summary>
        /// Observers at arm's length notice a stealthed creature more easily and more often.
        /// </summary>
        private static bool IsWithinCloseDetectionRange(uint observer, uint target)
        {
            return GetArea(observer) == GetArea(target) &&
                   GetDistanceBetween(observer, target) <= CloseRangeDetectionDistance;
        }

        private static bool ResolveDetection(uint observer, uint target, bool acquireAggroOnDetection)
        {
            var detected = GetOrRollVerdict(observer, target);
            EspionageInfiltration.RecordDetection(observer, target, detected);

            if (detected)
            {
                // Hostility is judged from the observer's perspective. A friendly or neutral
                // observer still sees the target (the verdict stays true), but only a hostile
                // observer's success ends the target's stealth for everyone.
                var hostile = GetIsEnemy(target, observer);

                if (ShouldBreakStealthOnDetection(true, hostile))
                    ExitDetectedPlayerStealth(observer, target);
                if (acquireAggroOnDetection && hostile)
                    AI.TryAcquireAggroAfterDetection(observer, target);
            }

            return detected;
        }

        /// <summary>
        /// Only a successful detection by a hostile observer globally breaks stealth.
        /// </summary>
        public static bool ShouldBreakStealthOnDetection(bool detected, bool observerIsHostile)
        {
            return detected && observerIsHostile;
        }

        /// <summary>
        /// A hostile observer's successful detection reveals a player to everyone by ending their
        /// stealth mode. NPC stealth keeps the engine's observer-specific behavior so creature
        /// encounters are not globally revealed when a single observer succeeds.
        /// </summary>
        private static void ExitDetectedPlayerStealth(uint observer, uint target)
        {
            if (!GetIsPC(target) ||
                GetIsDM(target) ||
                !GetActionMode(target, ActionMode.Stealth))
                return;

            Log.WriteStructured(
                LogGroup.AI,
                "Stealth detection forced native stealth exit: Observer={Observer} Target={Target}",
                observer,
                target);

            // Set the mode directly while the Spot hook is active, then retry after the native
            // detection call has unwound. Hostile AI can immediately start combat from a
            // successful verdict, and the deferred pass prevents that transition from leaving
            // the player's native mode and tracked status out of sync.
            SetActionMode(target, ActionMode.Stealth, false);
            StatusEffect.RemoveStatusEffect<StealthStatusEffect>(target);
            DelayCommand(0f, () =>
            {
                if (!GetIsObjectValid(target) || !GetActionMode(target, ActionMode.Stealth))
                    return;

                SetActionMode(target, ActionMode.Stealth, false);
                StatusEffect.RemoveStatusEffect<StealthStatusEffect>(target);
            });
            SendMessageToPC(target, ColorToken.Red("You have been detected and are forced out of stealth."));
        }

        private static void ClearVerdictsForTarget(uint target)
        {
            var stale = _verdicts.Keys.Where(k => k.Target == target).ToList();
            foreach (var key in stale)
            {
                _verdicts.Remove(key);
            }
        }

        private static void PruneExpired(DateTime now)
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
    }
}
