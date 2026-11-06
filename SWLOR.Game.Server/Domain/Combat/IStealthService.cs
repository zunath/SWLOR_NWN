using SWLOR.Game.Server.Core;
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

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>Replaces the base game's stealth detection with a single opposed check per observer/target pair.</summary>
    public interface IStealthService
    {
        /// <summary>
        /// Baseline stealth is only usable by characters with the Stealth perk and only out of
        /// combat. NPCs pass through so spawn tables can still field stealthy creatures.
        /// </summary>
        void GateStealthEntry();

        void OnStealthEntered();

        /// <summary>
        /// Rebuilds the active Stealth status after a rank purchase or refund so the status never
        /// retains a stat snapshot from the previous perk level. A full refund also exits the
        /// native action mode immediately because the ownership gate no longer passes.
        /// </summary>
        void RefreshActiveStatusAfterPerkLevelChange(uint creature);

        /// <summary>
        /// The Stealth status carries the stamina drain, so a stealthed player must never be
        /// without it. Rebuilds the status if something removed it while native stealth stayed on.
        /// </summary>
        void EnsureStealthStatusWhileHidden();

        /// <summary>
        /// Records player-initiated combat before the attack event can add pair enmity. This lets
        /// infiltration distinguish the attack from combat caused by a successful detection.
        /// </summary>
        void RecordPlayerCombatInitiation();

        /// <summary>
        /// Landing a hit is a hostile action, so it reveals the attacker. Abilities flagged
        /// BreaksStealth are already handled on activation; this covers auto-attacks and any
        /// damage-dealing path that does not route through an ability.
        /// </summary>
        void BreakStealthOnDamageDealt();

        /// <summary>
        /// Ends the stealth an ability grants after <paramref name="durationSeconds"/>. Leaving
        /// stealth first releases the window, so a later stealth entry is never ended by this timer.
        /// </summary>
        int ScheduleTimedExit(uint creature, float durationSeconds);

        /// <summary>
        /// Lets a player without the Stealth perk enter stealth for <paramref name="durationSeconds"/>
        /// (Shared Presence). The granted stealth uses <paramref name="stealthBonus"/> in place of the
        /// player's own perk bonus, drains no stamina, earns no bypass XP, and ends on the timer, on
        /// detection or hostile action, or when the player leaves stealth. Returns false when the
        /// player cannot be hidden now (not a player, dead, in combat, or already in stealth).
        /// </summary>
        bool TryGrantStealth(uint creature, int stealthBonus, float durationSeconds);

        /// <summary>
        /// Only a living player out of combat who is not already hidden can be granted stealth.
        /// A player already in stealth keeps their own stealth and its rules.
        /// </summary>
        bool CanReceiveGrantedStealth(bool isPlayer, bool isDead, bool isInCombat, bool isInStealth);

        /// <summary>
        /// Returns whether the creature's current stealth window was granted by another player.
        /// </summary>
        bool HasGrantedStealth(uint creature);

        /// <summary>
        /// The granting caster's Stealth perk bonus, used in place of the member's own.
        /// </summary>
        int GetGrantedStealthBonus(uint creature);

        /// <summary>
        /// Local variables are saved with the character, but the window's timer belongs to the old
        /// creature object, so a grant never outlives the session that received it.
        /// </summary>
        void ClearGrantedStealthOnEnter();

        void ClearGrantedStealthOnExit();

        /// <summary>
        /// A granted window ends with the player's death so a quick respawn cannot reuse it.
        /// </summary>
        void ClearGrantedStealthOnDeath();

        void OnStealthExited();

        /// <summary>
        /// The engine only raises this event when the target is stealthed (invisibility is resolved
        /// before the event fires), so every call is a real stealth-vs-detection question.
        /// </summary>
        void ResolveSpotDetection();

        /// <summary>
        /// Prevents the custom hostile aggro aura from bypassing native stealth visibility. The
        /// aura itself is positional and fires even when the hostile has not detected the player.
        /// </summary>
        bool CanAcquireAggro(uint observer, uint target);

        /// <summary>
        /// Returns whether the observer cannot see the target, either through invisibility or
        /// because the target is a stealthed player the observer has not detected.
        /// </summary>
        bool IsHiddenFrom(uint observer, uint target);

        /// <summary>
        /// Returns whether a stealthed player is hidden from the observer. An observer that could
        /// not make a native Spot check (out of sight range or no line of sight) cannot find the
        /// player; otherwise this uses the same cached Detection vs Stealth verdict as spot
        /// detection, so a successful hostile detection still reveals the player.
        /// </summary>
        bool IsHiddenByStealth(uint observer, uint target);

        /// <summary>
        /// Returns whether the creature is hidden from other players: in stealth or invisible. Signals
        /// that would tell nearby players such a creature is there go only to the creature itself.
        /// </summary>
        bool IsConcealed(uint creature);

        /// <summary>
        /// Returns whether a creature has an invisibility effect.
        /// </summary>
        bool IsInvisible(uint creature);

        /// <summary>
        /// Returns whether invisibility hides the target from the observer. See Invisibility and
        /// True Seeing let the observer see through it. Listen detection is suppressed, so a hidden
        /// target cannot be located by sound either.
        /// </summary>
        bool IsHiddenByInvisibility(uint observer, uint target);

        void SuppressListenDetection();

        /// <summary>
        /// Detection beats Stealth on a higher total; ties go to the hidden creature. For a player
        /// observer a natural 20 always detects, so no Stealth lead hides a creature from players
        /// entirely. NPC observers have no such floor, keeping capped NPC Detection unable to
        /// find a fully committed sneak.
        /// </summary>
        bool IsDetectionRollSuccessful(int detectionDie, int detection, int stealthDie, int stealth, bool naturalTwentyDetects);

        /// <summary>
        /// A clouded observer (Cloud Mind) cannot spot anyone in stealth. A clouded player still
        /// spots on a natural 20, so stealth is never absolute against a player.
        /// </summary>
        bool IsCloudedDetectionRollSuccessful(int detectionDie, bool playerObserver);

        /// <summary>
        /// Returns whether the observer cannot spot stealth (for example under Cloud Mind).
        /// </summary>
        bool IsStealthDetectionBlocked(uint observer);

        int GetCloseRangeDetectionBonus(bool closeRange);

        float GetDetectionCheckIntervalSeconds(bool closeRange);

        /// <summary>
        /// Only a successful detection by a hostile observer globally breaks stealth.
        /// </summary>
        bool ShouldBreakStealthOnDetection(bool detected, bool observerIsHostile);

        /// <summary>
        /// Drops every cached verdict the observer rolled, so a change to how the observer sees
        /// (Cloud Mind starting or ending) applies on its next check instead of after the cache expires.
        /// </summary>
        void ClearVerdictsForObserver(uint observer);

        /// <summary>
        /// An NPC observer that just lost sight of stealthed targets stops attacking them now
        /// instead of on its next combat round. Deferred out of the status application.
        /// </summary>
        void ReevaluateHiddenTargetsForObserver(uint observer);
    }
}
