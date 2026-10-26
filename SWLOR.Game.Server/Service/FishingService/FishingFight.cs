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

        /// <summary>
        /// How the fight ended when its own engine settled it (landed, snapped or escaped), which is what the window has shown. Null while
        /// the fight goes on, and when the angler abandoned it instead.
        /// </summary>
        public FishingFightResult Result { get; internal set; }

        /// <summary>
        /// The most Agriculture XP still to pay when the fight ends on its own: the bite's multiplied XP less what the bite already paid.
        /// It is paid in proportion to how long the fight lasted (<c>LineRules.XpShare</c>), counted only up to the turns taken (<c>LineRules.CountedFightSeconds</c>), and an abandoned fight never pays it.
        /// </summary>
        public int XpOnFinish { get; internal set; }

        /// <summary>When the fish bit and the fight began.</summary>
        public DateTime StartedAt { get; internal set; }

        /// <summary>The earliest time the angler's next action may resolve; null until they have taken the first, which is free.</summary>
        public DateTime? NextTurnAt { get; internal set; }

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
