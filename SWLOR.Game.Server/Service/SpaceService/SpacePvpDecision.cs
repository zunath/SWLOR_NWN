namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The outcome of asking whether one ship may attack another (plan section 9, PvP).
    /// </summary>
    public enum SpacePvpDecision
    {
        /// <summary>One side is not a player ship; the ground rules for NPCs apply and PvP rules do not.</summary>
        NotPlayerVersusPlayer = 0,

        /// <summary>Both ships are players ships, the attacker marked the target hostile and the area allows PvP.</summary>
        Allowed = 1,

        /// <summary>The attacker has not marked the target hostile in the player list.</summary>
        NotHostile = 2,

        /// <summary>The area PvP setting forbids players from fighting.</summary>
        AreaForbids = 3,

        /// <summary>The area allows PvP only between players who are not in the same party.</summary>
        PartyProtected = 4,
    }
}
