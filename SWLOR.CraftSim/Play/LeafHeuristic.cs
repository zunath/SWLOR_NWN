using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.CraftSim.Play
{
    /// <summary>One way to finish the item, taken from an available action's preview: what repeating that action costs per turn.</summary>
    /// <param name="ProgressPerTurn">Expected progress gained per use.</param>
    /// <param name="IntegrityLossPerTurn">Expected integrity lost per use, never negative.</param>
    /// <param name="CpPerTurn">CP spent per use.</param>
    public readonly record struct FinishRoute(double ProgressPerTurn, double IntegrityLossPerTurn, double CpPerTurn);

    /// <summary>
    /// What the previews of a position say about finishing the item: which available actions add progress, and what repeating each
    /// one costs. It is built from <c>PreviewAll()</c> only, so it works for any engine.
    /// </summary>
    public sealed class FinishEstimate
    {
        public static readonly FinishEstimate None = new(Array.Empty<FinishRoute>());

        public FinishEstimate(IReadOnlyList<FinishRoute> routes)
        {
            Routes = routes;
        }

        public IReadOnlyList<FinishRoute> Routes { get; }

        public static FinishEstimate From(IReadOnlyList<MinigameActionPreview> previews)
        {
            var routes = new List<FinishRoute>();
            foreach (var preview in previews)
            {
                if (!preview.Enabled)
                    continue;

                double progress = 0, integrityLoss = 0;
                foreach (var outcome in preview.Outcomes)
                {
                    var delta = outcome.Delta;
                    var weight = outcome.ChancePercent / 100.0;
                    progress += weight * delta.Progress;
                    integrityLoss += weight * -delta.Integrity;
                }

                if (progress > 0)
                    routes.Add(new FinishRoute(progress, Math.Max(0, integrityLoss), preview.CpCost));
            }

            return new FinishEstimate(routes);
        }

        /// <summary>
        /// How likely the item is to be finished with the integrity and CP left, from 0 to 1: the best route's share of the resources it
        /// would use, with <paramref name="margin"/> as the spare integrity that makes it safe. The turn that completes the item costs
        /// no integrity that matters, because completing the progress meter succeeds before the integrity meter is checked.
        /// </summary>
        public double Safety(MinigameMeters meters, double margin)
        {
            var remaining = meters.Progress.Max - meters.Progress.Value;
            if (remaining <= 0)
                return 1;

            var best = 0.0;
            foreach (var route in Routes)
            {
                var turns = remaining / route.ProgressPerTurn;
                var integrityNeeded = Math.Max(0, turns - 1) * route.IntegrityLossPerTurn;
                var cpNeeded = Math.Max(0, turns - 1) * route.CpPerTurn + route.CpPerTurn;

                var integrity = integrityNeeded <= 0 ? 1.0 : Math.Clamp(meters.Integrity.Value / (integrityNeeded * margin), 0, 1);
                var cp = cpNeeded <= 0 ? 1.0 : Math.Clamp(meters.Cp.Value / cpNeeded, 0, 1);
                best = Math.Max(best, integrity * cp);
            }

            return best;
        }
    }

    /// <summary>A position a search has reached: the shared meters and status after a result, and what the position's previews say about finishing.</summary>
    public sealed class LeafPosition
    {
        private static readonly IReadOnlyDictionary<string, int> NoValues = new Dictionary<string, int>();

        public LeafPosition(MinigameMeters meters, MinigameStatus status, FinishEstimate finish, IReadOnlyDictionary<string, int> meterValues = null)
        {
            Meters = meters;
            Status = status;
            Finish = finish;
            MeterValues = meterValues ?? NoValues;
        }

        public MinigameMeters Meters { get; }
        public MinigameStatus Status { get; }
        public FinishEstimate Finish { get; }

        /// <summary>
        /// The value of every meter the engine displays, by meter id, shared and engine-specific. It lets a minigame's own heuristic read a
        /// meter the shared four do not carry, such as the Kitchen's distance from its target. Empty for a position that ended the attempt.
        /// </summary>
        public IReadOnlyDictionary<string, int> MeterValues { get; }

        /// <summary>The value of one displayed meter, or null when the position does not carry it.</summary>
        public int? Meter(string id) => MeterValues.TryGetValue(id, out var value) ? value : null;

        public static LeafPosition Of(ICraftMinigameEngine engine) =>
            new(
                engine.Meters,
                engine.Status,
                engine.Status == MinigameStatus.InProgress ? FinishEstimate.From(engine.PreviewAll()) : FinishEstimate.None,
                engine.DescribeMeters().ToDictionary(view => view.Id, view => view.Value));
    }

    /// <summary>Scores a position from 0 (the item is lost) to 1 (a finished item of full quality), for the adaptive player's lookahead and the solver's beam.</summary>
    public interface ILeafHeuristic
    {
        double Evaluate(LeafPosition position);
    }

    /// <summary>
    /// The default heuristic: a finished item is worth <see cref="SuccessBonus"/> plus the rest in proportion to its quality, a broken
    /// piece is worth nothing, and an attempt still going is worth that same value weighted by how safely it can still be finished,
    /// with a small pull toward progress so equal positions are not shuffled forever. It reads only the shared meters and previews.
    /// </summary>
    public sealed class DefaultLeafHeuristic : ILeafHeuristic
    {
        public static readonly DefaultLeafHeuristic Instance = new();

        public DefaultLeafHeuristic(double successBonus = 0.5, double safetyMargin = 1.5, double progressPull = 0.02)
        {
            if (successBonus < 0 || successBonus >= 1)
                throw new ArgumentOutOfRangeException(nameof(successBonus), "The bonus for making the item is from 0 up to but not including 1.");
            if (safetyMargin < 1)
                throw new ArgumentOutOfRangeException(nameof(safetyMargin), "The safety margin cannot be below 1.");
            if (progressPull < 0 || progressPull >= 1)
                throw new ArgumentOutOfRangeException(nameof(progressPull), "The pull toward progress is from 0 up to but not including 1.");

            SuccessBonus = successBonus;
            SafetyMargin = safetyMargin;
            ProgressPull = progressPull;
        }

        /// <summary>The part of a finished item's value that does not depend on its quality; 0 values quality alone.</summary>
        public double SuccessBonus { get; }

        /// <summary>How much more integrity than the finish needs counts as safe.</summary>
        public double SafetyMargin { get; }

        /// <summary>The weight of progress alone in an unfinished position's value.</summary>
        public double ProgressPull { get; }

        /// <summary>The value of a finished item of this quality.</summary>
        public double FinishedValue(double qualityFraction) => SuccessBonus + (1 - SuccessBonus) * qualityFraction;

        public double Evaluate(LeafPosition position)
        {
            switch (position.Status)
            {
                case MinigameStatus.Failed:
                    return 0;
                case MinigameStatus.Succeeded:
                    return FinishedValue(position.Meters.Quality.Fraction);
            }

            var safety = position.Finish.Safety(position.Meters, SafetyMargin);
            return (1 - ProgressPull) * safety * FinishedValue(position.Meters.Quality.Fraction) +
                   ProgressPull * position.Meters.Progress.Fraction;
        }
    }
}
