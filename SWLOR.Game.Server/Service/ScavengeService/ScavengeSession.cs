using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;

namespace SWLOR.Game.Server.Service.ScavengeService
{
    /// <summary>How a session was played.</summary>
    public enum ScavengeMode
    {
        /// <summary>On the board, one action at a time.</summary>
        Board = 0,

        /// <summary>By Quick Search, in one go.</summary>
        Quick = 1
    }

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
        Replaced = 7
    }

    /// <summary>How a session ended and what the scavenger is told.</summary>
    /// <param name="End">Why it ended.</param>
    /// <param name="Finds">The items the site gave.</param>
    /// <param name="Message">The outcome without colour tokens, for the window.</param>
    public sealed record ScavengeResult(ScavengeEnd End, IReadOnlyList<ScavengeFind> Finds, string Message);

    /// <summary>
    /// One scavenger's visit to one site: the request that started it, the board they play, and what it has found and paid so far. The
    /// lifecycle settles it once.
    /// </summary>
    public sealed class ScavengeSession
    {
        private readonly List<ScavengeFind> _finds = new();

        public ScavengeSession(string id, ScavengeRequest request, SearchEngine engine)
        {
            Id = id;
            Request = request ?? throw new ArgumentNullException(nameof(request));
            Engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        public string Id { get; }

        public ScavengeRequest Request { get; }

        public uint Player => Request.Player;

        public uint Site => Request.Site;

        public SearchEngine Engine { get; }

        public ScavengeMode Mode { get; internal set; }

        /// <summary>What the site has given so far, in the order it was found.</summary>
        public IReadOnlyList<ScavengeFind> Finds => _finds;

        /// <summary>The Gathering XP paid so far.</summary>
        public int XpPaid { get; internal set; }

        /// <summary>When the session began.</summary>
        public DateTime StartedAt { get; internal set; }

        /// <summary>The earliest time the scavenger's next action may resolve; null until they have taken the first, which is free.</summary>
        public DateTime? NextActionAt { get; internal set; }

        /// <summary>True once the session is over, however it ended. Nothing more can happen to it.</summary>
        public bool Settled { get; private set; }

        /// <summary>How the session ended, or null while it goes on.</summary>
        public ScavengeResult Result { get; internal set; }

        internal void AddFind(ScavengeFind find) => _finds.Add(find);

        /// <summary>Marks the session over. Returns false when it already was, so a repeated settle does nothing.</summary>
        internal bool TrySettle()
        {
            if (Settled)
                return false;

            Settled = true;
            return true;
        }
    }
}
