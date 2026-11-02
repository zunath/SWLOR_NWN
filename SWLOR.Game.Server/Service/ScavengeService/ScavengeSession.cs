using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;

namespace SWLOR.Game.Server.Service.ScavengeService
{
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

        public ScavengeRequest Request { get; private set; }

        public uint Player => Request.Player;

        public uint Site => Request.Site;

        public SearchEngine Engine { get; private set; }

        /// <summary>True once a Dig Deeper find has had the Jackpot weight, which only the first find of a board gets.</summary>
        internal bool JackpotSpent { get; set; }

        /// <summary>Replaces the engine and the perks it was built from. Only the lifecycle does this, before the scavenger's first action.</summary>
        internal void Reconfigure(ScavengeRequest request, SearchEngine engine)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            Engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        public ScavengeMode Mode { get; internal set; }

        /// <summary>What the site has given so far, in the order it was found.</summary>
        public IReadOnlyList<ScavengeFind> Finds => _finds;

        /// <summary>The Gathering XP paid so far.</summary>
        public int XpPaid { get; internal set; }

        /// <summary>When the session began.</summary>
        public DateTime StartedAt { get; internal set; }

        /// <summary>When the scavenger last acted on the board (or began the visit). A board left idle for too long releases its site.</summary>
        public DateTime LastActivityAt { get; internal set; }

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
