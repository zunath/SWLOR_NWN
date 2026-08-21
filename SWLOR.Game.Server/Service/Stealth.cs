using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

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

        private const float DetectionCheckIntervalSeconds = 30f;
        private const int CachePruneThreshold = 2000;

        private static readonly Dictionary<(uint Observer, uint Target), (bool Detected, DateTime Expiry)> _verdicts = new();

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

                StatusEffect.RemoveStatusEffect<StealthStatusEffect>(creature);
                return;
            }

            ClearVerdictsForTarget(creature);
            StatusEffect.ApplyStatusEffect<StealthStatusEffect>(creature, creature, 0f);
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

            StatusEffect.RemoveStatusEffect<StealthStatusEffect>(creature);

            if (!GetActionMode(creature, ActionMode.Stealth))
                return;

            if (Perk.GetPerkLevel(creature, PerkType.Stealth) <= 0)
            {
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

        [NWNEventHandler(ScriptName.OnStealthExitAfter)]
        public static void OnStealthExited()
        {
            var creature = OBJECT_SELF;

            EspionageInfiltration.CancelPlayer(creature);
            StatusEffect.RemoveStatusEffect<StealthStatusEffect>(creature);
            ClearVerdictsForTarget(creature);
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

            var detected = ResolveDetection(observer, target);
            EventsPlugin.SetEventResult(detected ? "1" : "0");
        }

        /// <summary>
        /// Prevents the custom hostile aggro aura from bypassing native stealth visibility. The
        /// aura itself is positional and fires even when the hostile has not detected the player.
        /// </summary>
        public static bool CanAcquireAggro(uint observer, uint target)
        {
            if (!GetIsPC(target) ||
                GetIsDM(target) ||
                !GetActionMode(target, ActionMode.Stealth))
            {
                return true;
            }

            return ResolveDetection(observer, target);
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

            if (_verdicts.TryGetValue(key, out var verdict) && verdict.Expiry > now)
                return verdict.Detected;

            var detectionRoll = Random.D20(1) + Stat.GetDetection(observer);
            var stealthRoll = Random.D20(1) + Stat.GetStealth(target);
            var detected = detectionRoll > stealthRoll;

            if (_verdicts.Count >= CachePruneThreshold)
                PruneExpired(now);

            _verdicts[key] = (detected, now.AddSeconds(DetectionCheckIntervalSeconds));
            return detected;
        }

        private static bool ResolveDetection(uint observer, uint target)
        {
            var detected = GetOrRollVerdict(observer, target);
            EspionageInfiltration.RecordDetection(observer, target, detected);

            if (detected)
                ExitDetectedPlayerStealth(target);

            return detected;
        }

        /// <summary>
        /// A successful detection reveals a player to everyone by ending their stealth mode. NPC
        /// stealth keeps the engine's observer-specific behavior so creature encounters are not
        /// globally revealed when a single observer succeeds.
        /// </summary>
        private static void ExitDetectedPlayerStealth(uint target)
        {
            if (!GetIsPC(target) ||
                GetIsDM(target) ||
                !GetActionMode(target, ActionMode.Stealth))
                return;

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
            var expired = _verdicts.Where(kvp => kvp.Value.Expiry <= now).Select(kvp => kvp.Key).ToList();
            foreach (var key in expired)
            {
                _verdicts.Remove(key);
            }
        }
    }
}
