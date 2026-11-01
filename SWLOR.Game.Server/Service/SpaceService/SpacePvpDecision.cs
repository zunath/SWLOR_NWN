namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The outcome of asking whether one ship may attack another (plan section 9, PvP).
    /// </summary>
    public enum SpacePvpDecision
    {
        /// <summary>One side is not a player ship; the ground rules for NPCs apply and PvP rules do not.</summary>
        NotPlayerVersusPlayer = 0,

        /// <summary>Both ships belong to players and the ground hostility check lets the attacker attack.</summary>
        Allowed = 1,

        /// <summary>
        /// The ground hostility check refuses: the attacker has not marked the target hostile, or the engine PvP
        /// setting of the area or server does not allow these two players to fight.
        /// </summary>
        NotHostile = 2,
    }
}
