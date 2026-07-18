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

            StatusEffect.ApplyStatusEffect<StealthStatusEffect>(creature, creature, 0f);
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

            var detected = GetOrRollVerdict(observer, target);
            EventsPlugin.SetEventResult(detected ? "1" : "0");
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
