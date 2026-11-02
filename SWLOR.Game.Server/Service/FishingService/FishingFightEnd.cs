namespace SWLOR.Game.Server.Service.FishingService
{
    /// <summary>Why a fight ended before the angler landed the fish, snapped the line or let the fish escape.</summary>
    public enum FishingFightEnd
    {
        /// <summary>The window was closed.</summary>
        WindowClosed = 1,

        /// <summary>The angler moved from where they cast.</summary>
        Moved = 2,

        /// <summary>The angler entered combat.</summary>
        Combat = 3,

        /// <summary>The angler died.</summary>
        Died = 4,

        /// <summary>The angler logged out or the window could not stay open.</summary>
        Left = 5,

        /// <summary>A new bite started while the fight was still going.</summary>
        Replaced = 6,

        /// <summary>The angler took the rod out of their hand.</summary>
        RodUnequipped = 7,

        /// <summary>Another activity, such as resting, replaced the angler's fishing busy state.</summary>
        Interrupted = 8,

        /// <summary>The angler kept the window open without acting for <see cref="FishingFightLifecycle.IdleSeconds"/>.</summary>
        Idle = 9
    }
}
