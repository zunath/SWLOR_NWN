using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;

namespace SWLOR.Game.Server.Service.ProspectingService
{
    /// <summary>
    /// One player working one node: the request that began it and the engine playing it. The lifecycle settles it once and holds the node
    /// for the player until then, so nobody else works the node meanwhile.
    /// </summary>
    public sealed class ProspectingSession
    {
        public ProspectingSession(string id, ProspectingRequest request, ProspectingEngine engine, int harvesterCharges)
        {
            Id = id;
            Request = request ?? throw new ArgumentNullException(nameof(request));
            Engine = engine ?? throw new ArgumentNullException(nameof(engine));
            HarvesterCharges = harvesterCharges;
        }

        public string Id { get; }

        public ProspectingRequest Request { get; private set; }

        public uint Player => Request.Player;

        public uint Node => Request.Node;

        public ProspectingEngine Engine { get; private set; }

        /// <summary>Replaces the engine and the perks it was built from. Only the lifecycle does this, before the player's first action.</summary>
        internal void Reconfigure(ProspectingRequest request, ProspectingEngine engine)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            Engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        /// <summary>True once the session has ended, however it ended. Nothing more can happen to it.</summary>
        public bool Settled { get; private set; }

        /// <summary>How the session ended on its own (Leave, the last unit, a collapse or the last charge), or null while it goes on or when it was abandoned.</summary>
        public ProspectingEnding? Finished { get; internal set; }

        /// <summary>Why the session was abandoned, or null while it goes on or when it ended on its own.</summary>
        public ProspectingEnd? Abandoned { get; internal set; }

        /// <summary>The words that say how the session ended, for the window.</summary>
        public string EndMessage { get; internal set; }

        /// <summary>The charges the harvester has left. It is the harvester's, not the engine's, so a Quick Harvest keeps it too.</summary>
        public int HarvesterCharges { get; internal set; }

        /// <summary>The action being carried out, or null when the player is free to choose one.</summary>
        public string Working { get; internal set; }

        /// <summary>True while a Quick Harvest is running.</summary>
        public bool QuickHarvesting { get; internal set; }

        /// <summary>True once the session has done anything: Quick Harvest is offered only before.</summary>
        public bool HasActed => Engine.Turn > 0 || QuickHarvesting || QuickHarvested > 0;

        /// <summary>The units a Quick Harvest has taken so far.</summary>
        public int QuickHarvested { get; internal set; }

        /// <summary>The units a Quick Harvest set out to take.</summary>
        public int QuickHarvestTotal { get; internal set; }

        /// <summary>The items handed to the player so far, to tell what an action added.</summary>
        public HarvestDelivery Delivered { get; internal set; }

        /// <summary>When the player last acted, or began: a session left untouched for too long is let go.</summary>
        public DateTime LastActivityAt { get; internal set; }

        /// <summary>The XP the session has paid so far.</summary>
        public int XpPaid { get; internal set; }

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
