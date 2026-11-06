using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using System.Collections.Generic;
using System.Linq;
using System;

namespace SWLOR.Game.Server.Service
{
    /// <summary>Replaces the base game's stealth detection with a single opposed check per observer/target pair. The logic lives in <see cref="IStealthService"/>; the event handlers stay here and every member forwards to the container's instance.</summary>
    public static class Stealth
    {
        public const string CombatEntryWindowVariable = "STEALTH_COMBAT_ENTRY_WINDOW";

        private static IStealthService Instance => FacadeAccessor.Resolve<IStealthService>();

        /// <summary>
        /// Baseline stealth is only usable by characters with the Stealth perk and only out of
        /// combat. NPCs pass through so spawn tables can still field stealthy creatures.
        /// </summary>
        [NWNEventHandler(ScriptName.OnStealthEnterBefore)]
        public static void GateStealthEntry() => Instance.GateStealthEntry();

        [NWNEventHandler(ScriptName.OnStealthEnterAfter)]
        public static void OnStealthEntered() => Instance.OnStealthEntered();

        /// <summary>
        /// Rebuilds the active Stealth status after a rank purchase or refund so the status never
        /// retains a stat snapshot from the previous perk level. A full refund also exits the
        /// native action mode immediately because the ownership gate no longer passes.
        /// </summary>
        public static void RefreshActiveStatusAfterPerkLevelChange(uint creature) => Instance.RefreshActiveStatusAfterPerkLevelChange(creature);

        /// <summary>
        /// The Stealth status carries the stamina drain, so a stealthed player must never be
        /// without it. Rebuilds the status if something removed it while native stealth stayed on.
        /// </summary>
        [NWNEventHandler(ScriptName.OnPlayerHeartbeat)]
        public static void EnsureStealthStatusWhileHidden() => Instance.EnsureStealthStatusWhileHidden();

        /// <summary>
        /// Records player-initiated combat before the attack event can add pair enmity. This lets
        /// infiltration distinguish the attack from combat caused by a successful detection.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureAttackBefore)]
        public static void RecordPlayerCombatInitiation() => Instance.RecordPlayerCombatInitiation();

        /// <summary>
        /// Landing a hit is a hostile action, so it reveals the attacker. Abilities flagged
        /// BreaksStealth are already handled on activation; this covers auto-attacks and any
        /// damage-dealing path that does not route through an ability.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSWLORDamage)]
        public static void BreakStealthOnDamageDealt() => Instance.BreakStealthOnDamageDealt();

        /// <summary>
        /// Ends the stealth an ability grants after <paramref name="durationSeconds"/>. Leaving
        /// stealth first releases the window, so a later stealth entry is never ended by this timer.
        /// </summary>
        public static int ScheduleTimedExit(uint creature, float durationSeconds) => Instance.ScheduleTimedExit(creature, durationSeconds);

        /// <summary>
        /// Lets a player without the Stealth perk enter stealth for <paramref name="durationSeconds"/>
        /// (Shared Presence). The granted stealth uses <paramref name="stealthBonus"/> in place of the
        /// player's own perk bonus, drains no stamina, earns no bypass XP, and ends on the timer, on
        /// detection or hostile action, or when the player leaves stealth. Returns false when the
        /// player cannot be hidden now (not a player, dead, in combat, or already in stealth).
        /// </summary>
        public static bool TryGrantStealth(uint creature, int stealthBonus, float durationSeconds) => Instance.TryGrantStealth(creature, stealthBonus, durationSeconds);

        /// <summary>
        /// Only a living player out of combat who is not already hidden can be granted stealth.
        /// A player already in stealth keeps their own stealth and its rules.
        /// </summary>
        public static bool CanReceiveGrantedStealth(bool isPlayer, bool isDead, bool isInCombat, bool isInStealth) => Instance.CanReceiveGrantedStealth(isPlayer, isDead, isInCombat, isInStealth);

        /// <summary>
        /// Returns whether the creature's current stealth window was granted by another player.
        /// </summary>
        public static bool HasGrantedStealth(uint creature) => Instance.HasGrantedStealth(creature);

        /// <summary>
        /// The granting caster's Stealth perk bonus, used in place of the member's own.
        /// </summary>
        public static int GetGrantedStealthBonus(uint creature) => Instance.GetGrantedStealthBonus(creature);

        /// <summary>
        /// Local variables are saved with the character, but the window's timer belongs to the old
        /// creature object, so a grant never outlives the session that received it.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void ClearGrantedStealthOnEnter() => Instance.ClearGrantedStealthOnEnter();

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void ClearGrantedStealthOnExit() => Instance.ClearGrantedStealthOnExit();

        /// <summary>
        /// A granted window ends with the player's death so a quick respawn cannot reuse it.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void ClearGrantedStealthOnDeath() => Instance.ClearGrantedStealthOnDeath();

        [NWNEventHandler(ScriptName.OnStealthExitAfter)]
        public static void OnStealthExited() => Instance.OnStealthExited();

        /// <summary>
        /// The engine only raises this event when the target is stealthed (invisibility is resolved
        /// before the event fires), so every call is a real stealth-vs-detection question.
        /// </summary>
        [NWNEventHandler(ScriptName.OnDoSpotDetectionBefore)]
        public static void ResolveSpotDetection() => Instance.ResolveSpotDetection();

        /// <summary>
        /// Prevents the custom hostile aggro aura from bypassing native stealth visibility. The
        /// aura itself is positional and fires even when the hostile has not detected the player.
        /// </summary>
        public static bool CanAcquireAggro(uint observer, uint target) => Instance.CanAcquireAggro(observer, target);

        /// <summary>
        /// Returns whether the observer cannot see the target, either through invisibility or
        /// because the target is a stealthed player the observer has not detected.
        /// </summary>
        public static bool IsHiddenFrom(uint observer, uint target) => Instance.IsHiddenFrom(observer, target);

        /// <summary>
        /// Returns whether a stealthed player is hidden from the observer. An observer that could
        /// not make a native Spot check (out of sight range or no line of sight) cannot find the
        /// player; otherwise this uses the same cached Detection vs Stealth verdict as spot
        /// detection, so a successful hostile detection still reveals the player.
        /// </summary>
        public static bool IsHiddenByStealth(uint observer, uint target) => Instance.IsHiddenByStealth(observer, target);

        /// <summary>
        /// Returns whether the creature is hidden from other players: in stealth or invisible. Signals
        /// that would tell nearby players such a creature is there go only to the creature itself.
        /// </summary>
        public static bool IsConcealed(uint creature) => Instance.IsConcealed(creature);

        /// <summary>
        /// Returns whether a creature has an invisibility effect.
        /// </summary>
        public static bool IsInvisible(uint creature) => Instance.IsInvisible(creature);

        /// <summary>
        /// Returns whether invisibility hides the target from the observer. See Invisibility and
        /// True Seeing let the observer see through it. Listen detection is suppressed, so a hidden
        /// target cannot be located by sound either.
        /// </summary>
        public static bool IsHiddenByInvisibility(uint observer, uint target) => Instance.IsHiddenByInvisibility(observer, target);

        [NWNEventHandler(ScriptName.OnDoListenDetectionBefore)]
        public static void SuppressListenDetection() => Instance.SuppressListenDetection();

        /// <summary>
        /// Detection beats Stealth on a higher total; ties go to the hidden creature. For a player
        /// observer a natural 20 always detects, so no Stealth lead hides a creature from players
        /// entirely. NPC observers have no such floor, keeping capped NPC Detection unable to
        /// find a fully committed sneak.
        /// </summary>
        public static bool IsDetectionRollSuccessful(int detectionDie, int detection, int stealthDie, int stealth, bool naturalTwentyDetects) => Instance.IsDetectionRollSuccessful(detectionDie, detection, stealthDie, stealth, naturalTwentyDetects);

        /// <summary>
        /// A clouded observer (Cloud Mind) cannot spot anyone in stealth. A clouded player still
        /// spots on a natural 20, so stealth is never absolute against a player.
        /// </summary>
        public static bool IsCloudedDetectionRollSuccessful(int detectionDie, bool playerObserver) => Instance.IsCloudedDetectionRollSuccessful(detectionDie, playerObserver);

        /// <summary>
        /// Returns whether the observer cannot spot stealth (for example under Cloud Mind).
        /// </summary>
        public static bool IsStealthDetectionBlocked(uint observer) => Instance.IsStealthDetectionBlocked(observer);

        public static int GetCloseRangeDetectionBonus(bool closeRange) => Instance.GetCloseRangeDetectionBonus(closeRange);

        public static float GetDetectionCheckIntervalSeconds(bool closeRange) => Instance.GetDetectionCheckIntervalSeconds(closeRange);

        /// <summary>
        /// Only a successful detection by a hostile observer globally breaks stealth.
        /// </summary>
        public static bool ShouldBreakStealthOnDetection(bool detected, bool observerIsHostile) => Instance.ShouldBreakStealthOnDetection(detected, observerIsHostile);

        /// <summary>
        /// Drops every cached verdict the observer rolled, so a change to how the observer sees
        /// (Cloud Mind starting or ending) applies on its next check instead of after the cache expires.
        /// </summary>
        public static void ClearVerdictsForObserver(uint observer) => Instance.ClearVerdictsForObserver(observer);

        /// <summary>
        /// An NPC observer that just lost sight of stealthed targets stops attacking them now
        /// instead of on its next combat round. Deferred out of the status application.
        /// </summary>
        public static void ReevaluateHiddenTargetsForObserver(uint observer) => Instance.ReevaluateHiddenTargetsForObserver(observer);
    }
}
