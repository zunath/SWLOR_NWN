using SWLOR.CraftSim.Play;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;

namespace SWLOR.CraftSim.Scenario.Kitchen
{
    /// <summary>
    /// The Kitchen's own leaf heuristic. It scores a position like <see cref="DefaultLeafHeuristic"/>, with one difference: while the
    /// dish is outside the target zone its quality is 0 and says nothing about which way to go, so a position is credited for how near
    /// the flavor is to the target (the Kitchen's Off Target meter, which the player sees), up to <see cref="GradientCap"/> of a perfect
    /// dish. Inside the zone the real quality takes over, because the credit never exceeds it by more than the cap.
    /// </summary>
    public sealed class KitchenLeafHeuristic : ILeafHeuristic
    {
        private readonly DefaultLeafHeuristic _base;

        public KitchenLeafHeuristic(double gradientCap = 0.35, double farDistance = 70)
        {
            if (gradientCap < 0 || gradientCap > 1)
                throw new ArgumentOutOfRangeException(nameof(gradientCap));
            if (farDistance <= 0)
                throw new ArgumentOutOfRangeException(nameof(farDistance));

            _base = DefaultLeafHeuristic.Instance;
            GradientCap = gradientCap;
            FarDistance = farDistance;
        }

        /// <summary>The largest share of a perfect dish's quality that nearness to the target earns on its own.</summary>
        public double GradientCap { get; }

        /// <summary>The distance from the target at which nearness earns nothing.</summary>
        public double FarDistance { get; }

        public double Evaluate(LeafPosition position)
        {
            if (position.Status != MinigameStatus.InProgress)
                return _base.Evaluate(position);

            double quality = position.Meters.Quality.Fraction;
            var distance = position.Meter(KitchenEngine.DistanceMeterId);
            if (distance != null)
            {
                var nearness = Math.Max(0, 1 - distance.Value / FarDistance);
                quality = Math.Max(quality, GradientCap * nearness);
            }

            var safety = position.Finish.Safety(position.Meters, _base.SafetyMargin);
            return (1 - _base.ProgressPull) * safety * _base.FinishedValue(quality) +
                   _base.ProgressPull * position.Meters.Progress.Fraction;
        }
    }
}
