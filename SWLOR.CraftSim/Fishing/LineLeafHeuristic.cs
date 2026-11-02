using SWLOR.CraftSim.Play;
using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.CraftSim.Fishing
{
    /// <summary>
    /// Scores a position of a fight from 0 (the fish is lost) to 1 (landed) for the adaptive player's lookahead. A landed fish is
    /// worth 1 and a lost one 0; a fight still going is worth the blend of how close the fish is, how much of the line's strength is
    /// left and how worn down the fish is. It reads only the shared meters, which the Line maps to those three.
    /// </summary>
    public sealed class LineLeafHeuristic : ILeafHeuristic
    {
        public static readonly LineLeafHeuristic Instance = new();

        public LineLeafHeuristic(double closenessWeight = 0.45, double headroomWeight = 0.30, double wornWeight = 0.25, double ceiling = 0.95)
        {
            if (closenessWeight < 0 || headroomWeight < 0 || wornWeight < 0)
                throw new ArgumentOutOfRangeException(nameof(closenessWeight), "A weight cannot be negative.");
            if (closenessWeight + headroomWeight + wornWeight <= 0)
                throw new ArgumentException("At least one weight must be positive.");
            if (ceiling <= 0 || ceiling >= 1)
                throw new ArgumentOutOfRangeException(nameof(ceiling), "A fight still going is worth less than a landed fish.");

            ClosenessWeight = closenessWeight;
            HeadroomWeight = headroomWeight;
            WornWeight = wornWeight;
            Ceiling = ceiling;
        }

        public double ClosenessWeight { get; }
        public double HeadroomWeight { get; }
        public double WornWeight { get; }
        public double Ceiling { get; }

        public double Evaluate(LeafPosition position)
        {
            switch (position.Status)
            {
                case MinigameStatus.Failed:
                    return 0;
                case MinigameStatus.Succeeded:
                    return 1;
            }

            var meters = position.Meters;
            var total = ClosenessWeight + HeadroomWeight + WornWeight;
            var blend = ClosenessWeight * meters.Progress.Fraction
                        + HeadroomWeight * meters.Integrity.Fraction
                        + WornWeight * meters.Quality.Fraction;
            return Ceiling * blend / total;
        }
    }
}
