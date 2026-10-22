using SWLOR.CraftSim.Rng;
using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.CraftSim.Play
{
    /// <summary>How the adaptive player searches. The starting values play a bounded lookahead; a scenario may deepen it or replace the heuristic.</summary>
    public sealed record AdaptivePolicyOptions
    {
        public static readonly AdaptivePolicyOptions Default = new();

        /// <summary>
        /// Actions looked ahead, counting the one chosen. 1 picks the action whose previewed results score best; each further
        /// level copies the engine for every result of every available action, so the cost grows with
        /// (actions x results x <see cref="ForecastSamples"/>) to the power of <c>Depth - 1</c>. Three is the least that sees a
        /// setup action (raise Thermal, build Load, retool) pay off: with two the player never sets up and plays far worse.
        /// </summary>
        public int Depth { get; init; } = 3;

        /// <summary>
        /// How many times a copied engine's forecast is rolled for each result. The forecast (the next opportunity, drift or fish move)
        /// is the only chance a result's preview does not list, so it is sampled; an engine whose forecast uses no chance is played once.
        /// </summary>
        public int ForecastSamples { get; init; } = 2;

        /// <summary>
        /// The most engine copies one decision may play. When it runs out, the results not yet played are scored from their previews.
        /// It keeps a search bounded and repeatable; raise it for a deeper search.
        /// </summary>
        public int SampleBudget { get; init; } = 4000;

        /// <summary>
        /// The share of value lost for each further action a line takes, so of two lines of equal worth the one that gets there in fewer
        /// actions wins and the player does not dither. Small enough that a few points of quality still justify an action.
        /// </summary>
        public double TimePreference { get; init; } = 0.005;

        /// <summary>Scores the positions the search reaches. The default weighs finishing safely and quality.</summary>
        public ILeafHeuristic Heuristic { get; init; } = DefaultLeafHeuristic.Instance;

        public void Validate()
        {
            if (Depth < 1 || Depth > 6)
                throw new ArgumentOutOfRangeException(nameof(Depth), "The adaptive player looks 1 to 6 actions ahead.");
            if (ForecastSamples < 1 || ForecastSamples > 32)
                throw new ArgumentOutOfRangeException(nameof(ForecastSamples), "A forecast is sampled 1 to 32 times.");
            if (SampleBudget < 1)
                throw new ArgumentOutOfRangeException(nameof(SampleBudget), "A decision needs a budget of at least one engine copy.");
            if (TimePreference < 0 || TimePreference >= 1)
                throw new ArgumentOutOfRangeException(nameof(TimePreference), "The time preference is from 0 up to but not including 1.");
            if (Heuristic == null)
                throw new ArgumentNullException(nameof(Heuristic));
        }

        public string Describe() =>
            $"depth {Depth}, forecast samples per result {ForecastSamples}, budget {SampleBudget} engine copies per decision, " +
            $"{TimePreference:P1} of value lost per extra action, heuristic {Heuristic.GetType().Name}";
    }

    /// <summary>
    /// The skilled player: a depth-limited expectimax over what <c>PreviewAll()</c> says each action can do. For every available
    /// action it takes the exact results and chances from the preview, copies the engine with <see cref="ICraftMinigameEngine.Fork"/> to
    /// reach the state each result leads to (forcing that result), and averages the best follow-up. It sees the same previews a player
    /// sees and never touches the real engine. The same seed always chooses the same actions.
    /// </summary>
    public sealed class AdaptivePolicy : ICraftPolicy
    {
        private const double Tie = 1e-9;

        private readonly AdaptivePolicyOptions _options;
        private readonly int _seed;

        public AdaptivePolicy(AdaptivePolicyOptions options = null, int seed = 1)
        {
            _options = options ?? AdaptivePolicyOptions.Default;
            _options.Validate();
            _seed = seed;
        }

        public string Name => "Adaptive";

        public string Choose(ICraftMinigameEngine engine)
        {
            var search = new Search(_options, SimRandom.ForRun(_seed, SimStreams.AdaptivePolicy, engine.Turn));
            return search.Best(engine, engine.PreviewAll(), _options.Depth).ActionId;
        }

        private readonly record struct Pick(string ActionId, double Value);

        private sealed class Search
        {
            private readonly AdaptivePolicyOptions _options;
            private readonly ILeafHeuristic _heuristic;
            private readonly SimRandom _forecasts;
            private int _budget;

            public Search(AdaptivePolicyOptions options, SimRandom forecasts)
            {
                _options = options;
                _heuristic = options.Heuristic;
                _forecasts = forecasts;
                _budget = options.SampleBudget;
            }

            /// <summary>The available action with the highest expected value, preferring the cheaper one on a tie; a null id when none is available.</summary>
            public Pick Best(ICraftMinigameEngine node, IReadOnlyList<MinigameActionPreview> previews, int depth)
            {
                var finish = FinishEstimate.From(previews);
                string bestId = null;
                var bestValue = 0.0;
                var bestCp = int.MaxValue;

                foreach (var preview in previews)
                {
                    if (!preview.Enabled)
                        continue;

                    var value = depth <= 1 ? FromPreview(node, preview, finish) : ByFork(node, preview, finish, depth);
                    if (bestId == null || value > bestValue + Tie || (Math.Abs(value - bestValue) <= Tie && preview.CpCost < bestCp))
                    {
                        bestId = preview.Action.Id;
                        bestValue = value;
                        bestCp = preview.CpCost;
                    }
                }

                return new Pick(bestId, bestValue);
            }

            /// <summary>The last level: every result is scored from the meters the preview says it leaves.</summary>
            private double FromPreview(ICraftMinigameEngine node, MinigameActionPreview preview, FinishEstimate finish)
            {
                var total = 0.0;
                foreach (var outcome in preview.Outcomes)
                    total += outcome.ChancePercent / 100.0 * _heuristic.Evaluate(PositionAfter(node, outcome, finish));

                return total;
            }

            private double ByFork(ICraftMinigameEngine node, MinigameActionPreview preview, FinishEstimate finish, int depth)
            {
                var total = 0.0;
                var lowerRoll = 1;
                for (var index = 0; index < preview.Outcomes.Count; index++)
                {
                    var outcome = preview.Outcomes[index];
                    total += outcome.ChancePercent / 100.0 * OutcomeValue(node, preview, index, lowerRoll, finish, depth);
                    lowerRoll += outcome.ChancePercent;
                }

                return total;
            }

            private double OutcomeValue(
                ICraftMinigameEngine node, MinigameActionPreview preview, int index, int lowerRoll, FinishEstimate finish, int depth)
            {
                var outcome = preview.Outcomes[index];

                // A result that ends the attempt is fully described by its preview.
                if (outcome.ResultingStatus != MinigameStatus.InProgress || _budget <= 0)
                    return _heuristic.Evaluate(PositionAfter(node, outcome, finish));

                var total = 0.0;
                var taken = 0;
                while (taken < _options.ForecastSamples && (taken == 0 || _budget > 0))
                {
                    _budget--;

                    var forecast = new SimMinigameRandom(new SimRandom(unchecked((long)_forecasts.NextUInt64())));

                    // The first roll of an action with several results chooses among them: force the one being valued.
                    var random = new ForcedOutcomeRandom(preview.Outcomes.Count > 1 ? lowerRoll : null, forecast);
                    var child = node.Fork(random);
                    if (!child.Perform(preview.Action.Id).Performed)
                        return _heuristic.Evaluate(PositionAfter(node, outcome, finish));

                    var value = child.Status == MinigameStatus.InProgress
                        ? Best(child, child.PreviewAll(), depth - 1).Value
                        : _heuristic.Evaluate(new LeafPosition(child.Meters, child.Status, FinishEstimate.None));
                    total += (1 - _options.TimePreference) * value;
                    taken++;

                    // A forecast that rolled nothing is the same every time.
                    if (forecast.Calls == 0)
                        break;
                }

                return total / taken;
            }

            private static LeafPosition PositionAfter(ICraftMinigameEngine node, MinigameOutcomePreview outcome, FinishEstimate finish) =>
                new(
                    node.Meters.Apply(outcome.Delta),
                    outcome.ResultingStatus,
                    outcome.ResultingStatus == MinigameStatus.InProgress ? finish : FinishEstimate.None);
        }

        /// <summary>
        /// A source of chance whose first roll is fixed to land on one result of an action, and whose later rolls (the forecast) come from
        /// a seeded stream.
        /// </summary>
        private sealed class ForcedOutcomeRandom : IMinigameRandom
        {
            private readonly IMinigameRandom _rest;
            private int? _first;

            public ForcedOutcomeRandom(int? firstRoll, IMinigameRandom rest)
            {
                _first = firstRoll;
                _rest = rest;
            }

            public int D100()
            {
                if (_first is { } roll)
                {
                    _first = null;
                    return roll;
                }

                return _rest.D100();
            }

            public int Next(int minInclusive, int maxExclusive) => _rest.Next(minInclusive, maxExclusive);
        }
    }
}
