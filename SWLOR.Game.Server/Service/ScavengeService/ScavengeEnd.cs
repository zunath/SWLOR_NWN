namespace SWLOR.Game.Server.Service.ScavengeService
{
    /// <summary>Why a session ended.</summary>
    public enum ScavengeEnd
    {
        /// <summary>The last search was spent.</summary>
        Completed = 0,

        /// <summary>Quick Search resolved the site.</summary>
        QuickSearch = 1,

        /// <summary>The scavenger closed the window.</summary>
        WindowClosed = 2,

        /// <summary>The scavenger moved away from the site.</summary>
        Moved = 3,

        /// <summary>The scavenger entered combat.</summary>
        Combat = 4,

        /// <summary>The scavenger died.</summary>
        Died = 5,

        /// <summary>The scavenger logged out.</summary>
        Left = 6,

        /// <summary>The scavenger started another session.</summary>
        Replaced = 7,

        /// <summary>The site was removed from the world while the board was open.</summary>
        SiteGone = 8,

        /// <summary>The scavenger stayed at the open board without acting for too long, so the site is released for others.</summary>
        Idle = 9
    }
}
