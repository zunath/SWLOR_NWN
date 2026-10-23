namespace SWLOR.Game.Server.Service.ProspectingService
{
    /// <summary>Why a session ended before the player finished it with Leave, a collapse, the last unit or the last charge.</summary>
    public enum ProspectingEnd
    {
        /// <summary>The window was closed.</summary>
        WindowClosed = 1,

        /// <summary>The player moved from where they began.</summary>
        Moved = 2,

        /// <summary>The player entered combat.</summary>
        Combat = 3,

        /// <summary>The player died.</summary>
        Died = 4,

        /// <summary>The player logged out or the window could not stay open.</summary>
        Left = 5,

        /// <summary>The player began another session while this one was still going.</summary>
        Replaced = 6,

        /// <summary>The harvester left the player's inventory.</summary>
        HarvesterLost = 7,

        /// <summary>Another activity, such as resting, replaced the player's harvesting busy state.</summary>
        Interrupted = 8,

        /// <summary>The node was removed from the world while it was being worked.</summary>
        NodeGone = 9
    }
}
