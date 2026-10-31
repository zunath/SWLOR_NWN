namespace SWLOR.Game.Server.Service.HuttballService
{
    /// <summary>
    /// Whether a Huttball player is in play, on the ground, or benched.
    /// </summary>
    public enum HuttballPlayerStatus
    {
        Standing = 1,
        KnockedDown = 2,
        /// <summary>Out until halftime.</summary>
        Injured = 3,
        /// <summary>Out for the rest of the match. Maimed players are out the same way.</summary>
        SeverelyInjured = 4,
        /// <summary>Sent off by the referee until halftime.</summary>
        SentOff = 5,
    }
}
