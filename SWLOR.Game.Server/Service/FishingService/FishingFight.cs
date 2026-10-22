using SWLOR.Game.Server.Service.CraftService.Minigame.Line;

namespace SWLOR.Game.Server.Service.FishingService
{
    /// <summary>One fight in progress: the angler, the fish that bit and the engine playing it. The lifecycle settles it once.</summary>
    public sealed class FishingFight
    {
        public FishingFight(string id, FishingBite bite, LineEngine engine)
        {
            Id = id;
            Bite = bite ?? throw new ArgumentNullException(nameof(bite));
            Engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        public string Id { get; }

        public FishingBite Bite { get; }

        public uint Player => Bite.Player;

        public FishAttribute Fish => Bite.Fish;

        public LineEngine Engine { get; }

        /// <summary>True once the fight has been landed, lost or abandoned. Nothing more can happen to it.</summary>
        public bool Settled { get; private set; }

        /// <summary>Marks the fight over. Returns false when it already was, so a repeated settle does nothing.</summary>
        internal bool TrySettle()
        {
            if (Settled)
                return false;

            Settled = true;
            return true;
        }
    }
}
