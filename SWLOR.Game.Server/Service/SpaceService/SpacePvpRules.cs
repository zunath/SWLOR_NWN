using SWLOR.Game.Server.Service.PvpCombatService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The pure rules for player ships fighting player ships (plan section 9, PvP). A ship may attack another
    /// player ship only when the ground rules allow it. The ground defers to the engine: abilities test
    /// <c>GetIsReactionTypeHostile(target, attacker)</c>, which depends on reputation, the area and server PvP
    /// setting and, between two players, the attacker Like/Dislike mark. Space asks the same question and does not
    /// read the PvP settings itself, so it cannot drift from the ground. Ships against NPC ships keep the engine
    /// reaction too.
    /// </summary>
    public static class SpacePvpRules
    {
        /// <param name="isPlayerVersusPlayer">Both ships belong to players.</param>
        /// <param name="groundHostile">The ground hostility check (<c>GetIsReactionTypeHostile</c>) says the attacker is hostile to the target.</param>
        public static SpacePvpDecision CanAttack(bool isPlayerVersusPlayer, bool groundHostile)
        {
            if (!isPlayerVersusPlayer)
                return SpacePvpDecision.NotPlayerVersusPlayer;

            return groundHostile ? SpacePvpDecision.Allowed : SpacePvpDecision.NotHostile;
        }

        /// <summary>Whether the decision lets the attack happen. Ships against NPCs are never stopped by PvP rules.</summary>
        public static bool IsAllowed(SpacePvpDecision decision) =>
            decision == SpacePvpDecision.Allowed || decision == SpacePvpDecision.NotPlayerVersusPlayer;

        /// <summary>
        /// Whether a ship is hostile for support purposes: remote repair, capacitor transfer and Command orders skip a
        /// ship that is an enemy, and between two players a ship that either side marked hostile in the player list.
        /// </summary>
        public static bool IsHostile(
            bool isPlayerVersusPlayer,
            bool engineEnemy,
            bool sourceMarkedTargetHostile,
            bool targetMarkedSourceHostile)
        {
            if (isPlayerVersusPlayer && (sourceMarkedTargetHostile || targetMarkedSourceHostile))
                return true;

            return engineEnemy;
        }

        /// <summary>The short message a pilot reads when a PvP decision stops their attack. Empty when it is allowed.</summary>
        public static string GetRefusal(SpacePvpDecision decision)
        {
            return decision == SpacePvpDecision.NotHostile
                ? "You cannot attack that player's ship: mark them hostile in the player list, and PvP must be allowed here."
                : string.Empty;
        }

        /// <summary>
        /// Raises ship damage by the target's PvP damage ramp. Only damage a player's ship deals to another player's
        /// ship is raised; the increase belongs to the player being hit, as on the ground.
        /// </summary>
        public static int ApplyRamp(int damage, bool isPlayerVersusPlayer, int rampPercent)
        {
            return isPlayerVersusPlayer ? PvpCombatRules.IncreaseDamage(damage, rampPercent) : damage;
        }
    }
}
