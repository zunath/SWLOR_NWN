using SWLOR.Game.Server.Service.PvpCombatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The pure rules for player ships fighting player ships (plan section 9, PvP): a ship may attack another
    /// player's ship only when the ground rules allow it, which is like/dislike hostility (the player list) plus the
    /// area's PvP setting. Ships against NPC ships keep the engine's reaction.
    /// </summary>
    public static class SpacePvpRules
    {
        /// <summary>
        /// Decides whether an attack may go ahead. <paramref name="areaSetting"/> must already be resolved: a
        /// <see cref="PvPSetting.ServerDefault"/> area takes the server's setting before this is called.
        /// </summary>
        /// <param name="isPlayerVersusPlayer">Both ships belong to players.</param>
        /// <param name="attackerMarkedTargetHostile">The attacker has the target marked hostile in the player list.</param>
        /// <param name="areaSetting">The area's effective PvP setting.</param>
        /// <param name="sameParty">The two players are in the same party.</param>
        public static SpacePvpDecision CanAttack(
            bool isPlayerVersusPlayer,
            bool attackerMarkedTargetHostile,
            PvPSetting areaSetting,
            bool sameParty)
        {
            if (!isPlayerVersusPlayer)
                return SpacePvpDecision.NotPlayerVersusPlayer;

            if (!attackerMarkedTargetHostile)
                return SpacePvpDecision.NotHostile;

            switch (areaSetting)
            {
                case PvPSetting.FullPvP:
                    return SpacePvpDecision.Allowed;
                case PvPSetting.PartyPvP:
                    return sameParty ? SpacePvpDecision.PartyProtected : SpacePvpDecision.Allowed;
                default:
                    return SpacePvpDecision.AreaForbids;
            }
        }

        /// <summary>Whether the decision lets the attack happen. Ships against NPCs are never stopped by PvP rules.</summary>
        public static bool IsAllowed(SpacePvpDecision decision) =>
            decision == SpacePvpDecision.Allowed || decision == SpacePvpDecision.NotPlayerVersusPlayer;

        /// <summary>
        /// Whether a ship is hostile for support purposes: remote repair, capacitor transfer and Command orders skip a
        /// ship that is an enemy, and between two players a ship that either side marked hostile.
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

        /// <summary>Resolves an area's PvP setting: a server-default area takes the server's setting.</summary>
        public static PvPSetting ResolveAreaSetting(PvPSetting areaSetting, PvPSetting serverSetting)
        {
            return areaSetting == PvPSetting.ServerDefault ? serverSetting : areaSetting;
        }

        /// <summary>The short message a pilot reads when a PvP decision stops their attack. Empty when it is allowed.</summary>
        public static string GetRefusal(SpacePvpDecision decision)
        {
            switch (decision)
            {
                case SpacePvpDecision.NotHostile:
                    return "You have not marked that player hostile, so you cannot attack their ship.";
                case SpacePvpDecision.AreaForbids:
                    return "Players cannot fight each other in this area.";
                case SpacePvpDecision.PartyProtected:
                    return "You cannot attack a member of your own party.";
                default:
                    return string.Empty;
            }
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
