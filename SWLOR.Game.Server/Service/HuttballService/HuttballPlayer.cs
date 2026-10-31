namespace SWLOR.Game.Server.Service.HuttballService
{
    /// <summary>
    /// One creature taking part in a Huttball match, and what it may still do this team turn.
    /// </summary>
    public class HuttballPlayer
    {
        public HuttballPlayer(uint creature, string name, HuttballTeam team, HuttballRole role)
        {
            Creature = creature;
            Name = name;
            Team = team;
            Role = role;
            Status = HuttballPlayerStatus.Standing;
        }

        public uint Creature { get; }
        public string Name { get; }
        public HuttballTeam Team { get; }
        public HuttballRole Role { get; set; }
        public HuttballPlayerStatus Status { get; set; }

        /// <summary>
        /// The square the player stands or lies on, or null while benched or not yet placed.
        /// </summary>
        public HuttballSquare? Square { get; set; }

        /// <summary>
        /// Squares the player may still move this team turn.
        /// </summary>
        public int MovementLeft { get; set; }

        /// <summary>
        /// True once the player has blocked, passed, or made a dirty move this team turn.
        /// </summary>
        public bool HasActed { get; set; }

        /// <summary>
        /// True once the player is finished for this team turn.
        /// </summary>
        public bool IsDone { get; set; }

        public HuttballRoleStats Stats => HuttballRules.GetStats(Role);

        /// <summary>
        /// True when the player is on the pitch, standing or knocked down.
        /// </summary>
        public bool IsOnPitch => Square != null &&
                                 (Status == HuttballPlayerStatus.Standing || Status == HuttballPlayerStatus.KnockedDown);

        public bool IsStandingOnPitch => Square != null && Status == HuttballPlayerStatus.Standing;

        public string DisplayName => $"{Name} ({Role})";
    }
}
