using SWLOR.CraftSim.Rng;
using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.CraftSim.Play
{
    /// <summary>How hard the fixed-rotation solver searches. Larger values make the adversary of the no-fixed-rotation gate stronger and slower.</summary>
    public sealed record FixedRotationSolverOptions
    {
        public static readonly FixedRotationSolverOptions Default = new();

        /// <summary>A longer search for a final run: more of everything. It takes several times as long and finds better rotations than <see cref="Default"/>.</summary>
        public static readonly FixedRotationSolverOptions Thorough = new()
        {
            BeamWidth = 12, TrainingRuns = 100, SelectionRuns = 250, Finalists = 16, PolishStarts = 5, PolishPasses = 4, AdaptiveSeedLines = 12
        };

        /// <summary>Sequences kept at each length by the beam search.</summary>
        public int BeamWidth { get; init; } = 8;

        /// <summary>The longest rotation searched, in actions.</summary>
        public int MaximumLength { get; init; } = 40;

        /// <summary>Seeded attempts every candidate is scored on while the search runs (the same dice for every candidate).</summary>
        public int TrainingRuns { get; init; } = 64;

        /// <summary>
        /// Different seeded attempts, more of them than the search used, that pick the winner among the finalists. A rotation the search
        /// tuned to its own dice looks better there than it is, so the winner is chosen on dice it never saw.
        /// </summary>
        public int SelectionRuns { get; init; } = 150;

        /// <summary>How many candidates go to the selection attempts: the local search's results first, then the best of the rest.</summary>
        public int Finalists { get; init; } = 12;

        /// <summary>Rounds of local search (replace, swap, delete or insert one action or a pair) on each start. Every round scores on attempts the earlier rounds did not use.</summary>
        public int PolishPasses { get; init; } = 3;

        /// <summary>How many of the best candidates the local search starts from.</summary>
        public int PolishStarts { get; init; } = 3;

        /// <summary>
        /// The longest repeating cycle tried: with 3, every cycle of 1, 2 or 3 actions (such as "stoke, press, polish" over and over) is
        /// scored. Rotations that repeat a short pattern are what players actually settle on.
        /// </summary>
        public int CycleLength { get; init; } = 3;

        /// <summary>Attempts used to screen the cycles before the best are scored on all <see cref="TrainingRuns"/>.</summary>
        public int ScreeningRuns { get; init; } = 12;

        /// <summary>
        /// Adaptive attempts whose action lines are tried as fixed rotations. A skilled line is the best guess at a good rotation,
        /// and the search then makes it robust. 0 turns it off; the gate supplies the lines.
        /// </summary>
        public int AdaptiveSeedLines { get; init; } = 8;

        public void Validate()
        {
            if (BeamWidth < 1 || BeamWidth > 256)
                throw new ArgumentOutOfRangeException(nameof(BeamWidth), "The beam keeps 1 to 256 sequences.");
            if (MaximumLength < 1 || MaximumLength > 200)
                throw new ArgumentOutOfRangeException(nameof(MaximumLength), "A rotation is 1 to 200 actions long.");
            if (TrainingRuns < 1 || SelectionRuns < 1 || ScreeningRuns < 1)
                throw new ArgumentOutOfRangeException(nameof(TrainingRuns), "Scoring needs at least one attempt.");
            if (Finalists < 1 || PolishStarts < 1)
                throw new ArgumentOutOfRangeException(nameof(Finalists), "At least one finalist and one start are needed.");
            if (PolishPasses < 0 || AdaptiveSeedLines < 0)
                throw new ArgumentOutOfRangeException(nameof(PolishPasses), "Passes and lines cannot be negative.");
            if (CycleLength < 0 || CycleLength > 5)
                throw new ArgumentOutOfRangeException(nameof(CycleLength), "Cycles are 0 to 5 actions long.");
        }

        public string Describe() =>
            $"beam {BeamWidth}, cycles up to {CycleLength}, {AdaptiveSeedLines} adaptive lines as seeds, up to {MaximumLength} actions, " +
            $"{TrainingRuns} training attempts per local-search round and {SelectionRuns} selection attempts for each of {Finalists} finalists, " +
            $"local search from the best {PolishStarts} candidate(s) for up to {PolishPasses} round(s) each";
    }

    /// <summary>The best fixed rotation found for one recipe, rank, gear and style.</summary>
    /// <param name="Sequence">The actions in order.</param>
    /// <param name="TrainingScore">Its mean score on the attempts it was searched on, which favors it.</param>
    /// <param name="SelectionScore">Its mean score on separate attempts, which it was only chosen on.</param>
    public sealed record SolvedRotation(IReadOnlyList<string> Sequence, double TrainingScore, double SelectionScore);

    /// <summary>
    /// The adversary of the no-fixed-rotation gate: it searches for the fixed sequence of actions with the best mean quality (a broken
    /// piece counts 0) for one recipe. Candidates come from four places, all scored on the same seeded attempts: a beam search that
    /// extends sequences one action at a time (an unfinished prefix ranked by a heuristic, a finished one by its exact result), every
    /// short repeating cycle of actions, and the action lines a skilled adaptive player takes. A local search then tries replacing,
    /// swapping, deleting and inserting single actions of the best few, scoring each variant on whole attempts. The best candidates are
    /// re-scored on different attempts and the winner is chosen there. An action that is unavailable when its turn comes is skipped, and
    /// the last action repeats once the list runs out.
    /// </summary>
    public sealed class FixedRotationSolver
    {
        private const double Improvement = 1e-9;

        private readonly FixedRotationSolverOptions _options;
        private readonly ILeafHeuristic _guidance;
        private readonly int _actionLimit;
        private readonly int _maximumLength;

        /// <param name="options">How hard to search.</param>
        /// <param name="guidance">Ranks unfinished prefixes in the beam; the default weighs finishing safely and quality.</param>
        /// <param name="actionLimit">
        /// The actions after which an attempt counts as stalled, the same limit the gate scores the winner with. Every attempt the search
        /// plays uses it, and no rotation is searched longer than it.
        /// </param>
        public FixedRotationSolver(FixedRotationSolverOptions options = null, ILeafHeuristic guidance = null, int actionLimit = CraftPlayer.DefaultActionLimit)
        {
            if (actionLimit < 1)
                throw new ArgumentOutOfRangeException(nameof(actionLimit), "An attempt needs at least one action.");

            _options = options ?? FixedRotationSolverOptions.Default;
            _options.Validate();
            _guidance = guidance ?? DefaultLeafHeuristic.Instance;
            _actionLimit = actionLimit;
            _maximumLength = Math.Min(_options.MaximumLength, actionLimit);
        }

        /// <summary>
        /// Searches for the best fixed rotation.
        /// </summary>
        /// <param name="factory">Builds an attempt that has not started, on the source of chance it is given.</param>
        /// <param name="seed">The seed every attempt's chance comes from.</param>
        /// <param name="stream">Numbers that tell this search's streams apart from other searches' (the recipe, gear and style).</param>
        /// <param name="seedLines">Action lines worth trying as rotations, such as an adaptive player's; may be null.</param>
        public SolvedRotation Solve(
            Func<IMinigameRandom, ICraftMinigameEngine> factory,
            int seed,
            int[] stream,
            IReadOnlyList<IReadOnlyList<string>> seedLines = null)
        {
            var actions = factory(new SimMinigameRandom(new SimRandom(0))).Actions.Select(action => action.Id).ToList();
            var trainingCaches = new Dictionary<int, Dictionary<string, double>>();

            // Round k of the local search scores on its own block of attempts, so a variant has to hold up on dice the last round did not use.
            Func<IReadOnlyList<string>, double> TrainFor(int round)
            {
                if (!trainingCaches.TryGetValue(round, out var cache))
                    trainingCaches[round] = cache = new Dictionary<string, double>();

                return sequence =>
                {
                    var key = Key(sequence);
                    if (!cache.TryGetValue(key, out var score))
                    {
                        score = Score(
                            factory, sequence, seed, SimStreams.SolverTraining, stream, _options.TrainingRuns, _actionLimit,
                            firstRun: round * _options.TrainingRuns);
                        cache[key] = score;
                    }

                    return score;
                };
            }

            var candidates = new List<List<string>>();
            candidates.AddRange(Beam(factory, actions, seed, stream));
            candidates.AddRange(Cycles(factory, actions, seed, stream));
            if (seedLines != null)
                candidates.AddRange(seedLines.Where(line => line.Count > 0 && line.All(actions.Contains)).Select(line => line.Take(_maximumLength).ToList()));

            var firstRound = TrainFor(0);
            var ranked = Distinct(candidates).OrderByDescending(sequence => firstRound(sequence)).ToList();
            var polished = ranked.Take(_options.PolishStarts).Select(start => Polish(start, actions, TrainFor)).ToList();

            var finalists = Distinct(polished.Concat(ranked)).Take(Math.Max(_options.Finalists, _options.PolishStarts)).ToList();

            List<string> winner = null;
            var winnerSelection = double.MinValue;
            foreach (var sequence in finalists)
            {
                var selection = Score(factory, sequence, seed, SimStreams.SolverSelection, stream, _options.SelectionRuns, _actionLimit);
                if (selection > winnerSelection + Improvement)
                {
                    winner = sequence;
                    winnerSelection = selection;
                }
            }

            return winner == null
                ? new SolvedRotation(new List<string>(), 0, 0)
                : new SolvedRotation(winner, firstRound(winner), winnerSelection);
        }

        /// <summary>The mean score of a fixed rotation over seeded attempts: quality of the item made, 0 for no item.</summary>
        public static double Score(
            Func<IMinigameRandom, ICraftMinigameEngine> factory,
            IReadOnlyList<string> sequence,
            int seed,
            int streamKind,
            int[] stream,
            int runs,
            int actionLimit,
            int firstRun = 0)
        {
            var total = 0.0;
            for (var run = firstRun; run < firstRun + runs; run++)
                total += PlayRun(factory, sequence, seed, streamKind, stream, run, actionLimit).Score;

            return total / runs;
        }

        public static CraftPlayResult PlayRun(
            Func<IMinigameRandom, ICraftMinigameEngine> factory,
            IReadOnlyList<string> sequence,
            int seed,
            int streamKind,
            int[] stream,
            int run,
            int actionLimit)
        {
            var engine = factory(new SimMinigameRandom(SimRandom.ForRun(seed, Stream(streamKind, stream, 0, run))));
            return CraftPlayer.PlayFixed(engine, sequence, actionLimit);
        }

        /// <summary>
        /// The action lines an adaptive player takes on attempts of their own, to hand to <see cref="Solve"/> as seeds. A skilled line is
        /// not itself a good fixed rotation (it reacts), but it shows which actions a good rotation uses and in what order.
        /// </summary>
        public static IReadOnlyList<IReadOnlyList<string>> AdaptiveLines(
            Func<IMinigameRandom, ICraftMinigameEngine> factory,
            AdaptivePolicyOptions options,
            int seed,
            int[] stream,
            int count,
            int actionLimit = CraftPlayer.DefaultActionLimit)
        {
            var lines = new List<IReadOnlyList<string>>();
            for (var run = 0; run < count; run++)
            {
                var engine = factory(new SimMinigameRandom(SimRandom.ForRun(seed, Stream(SimStreams.SolverSeedLines, stream, 0, run))));
                var recorder = new RecordingPolicy(new AdaptivePolicy(options, unchecked((int)SimRandom.ForRun(seed, Stream(SimStreams.SolverSeedLines, stream, 1, run)).NextUInt64())));
                CraftPlayer.Play(engine, recorder, actionLimit);
                if (recorder.Actions.Count > 0)
                    lines.Add(recorder.Actions);
            }

            return lines;
        }

        private sealed class RecordingPolicy : ICraftPolicy
        {
            private readonly ICraftPolicy _inner;
            private readonly List<string> _actions = new();

            public RecordingPolicy(ICraftPolicy inner)
            {
                _inner = inner;
            }

            public string Name => _inner.Name;
            public IReadOnlyList<string> Actions => _actions;

            public string Choose(ICraftMinigameEngine engine)
            {
                var action = _inner.Choose(engine);
                if (action != null)
                    _actions.Add(action);

                return action;
            }
        }

        private sealed class Entry
        {
            public List<string> Sequence;
            public ICraftMinigameEngine[] Engines;
            public double Score;
            public bool Finished;
        }

        /// <summary>Beam search: the finished sequences it met, best first, or the longest prefixes when none finished.</summary>
        private List<List<string>> Beam(Func<IMinigameRandom, ICraftMinigameEngine> factory, List<string> actions, int seed, int[] stream)
        {
            var samples = _options.TrainingRuns;
            var root = new Entry { Sequence = new List<string>(), Engines = new ICraftMinigameEngine[samples] };
            for (var run = 0; run < samples; run++)
                root.Engines[run] = factory(new SimMinigameRandom(SimRandom.ForRun(seed, Stream(SimStreams.SolverTraining, stream, 0, run))));

            var beam = new List<Entry> { root };
            var finished = new List<Entry>();
            var seen = new HashSet<int>();

            for (var step = 1; step <= _maximumLength && beam.Count > 0; step++)
            {
                var next = new List<Entry>();
                seen.Clear();
                foreach (var entry in beam)
                {
                    foreach (var action in actions)
                    {
                        var child = Extend(entry, action, step, seed, stream);
                        if (child == null)
                            continue;

                        if (child.Finished)
                        {
                            finished.Add(child);
                            continue;
                        }

                        if (seen.Add(Signature(child)))
                            next.Add(child);
                    }
                }

                beam = next.OrderByDescending(entry => entry.Score).Take(_options.BeamWidth).ToList();
            }

            return (finished.Count > 0 ? finished : beam)
                .OrderByDescending(entry => entry.Score)
                .Take(Math.Max(_options.Finalists * 2, _options.BeamWidth))
                .Select(entry => entry.Sequence)
                .ToList();
        }

        /// <summary>The entry after one more action on every training attempt that is still going, or null when the action changed nothing anywhere.</summary>
        private Entry Extend(Entry entry, string action, int step, int seed, int[] stream)
        {
            var engines = new ICraftMinigameEngine[entry.Engines.Length];
            var changed = false;
            var allEnded = true;
            var total = 0.0;

            for (var run = 0; run < engines.Length; run++)
            {
                var engine = entry.Engines[run];
                if (engine.Status == MinigameStatus.InProgress)
                {
                    var copy = engine.Fork(new SimMinigameRandom(SimRandom.ForRun(seed, Stream(SimStreams.SolverTraining, stream, step, run))));
                    if (copy.Perform(action).Performed)
                    {
                        engine = copy;
                        changed = true;
                    }
                }

                engines[run] = engine;
                switch (engine.Status)
                {
                    case MinigameStatus.Succeeded:
                        total += _guidance.Evaluate(LeafPosition.Of(engine));
                        break;
                    case MinigameStatus.InProgress:
                        allEnded = false;
                        total += _guidance.Evaluate(LeafPosition.Of(engine));
                        break;
                }
            }

            if (!changed)
                return null;

            return new Entry
            {
                Sequence = new List<string>(entry.Sequence) { action },
                Engines = engines,
                Score = total / engines.Length,
                Finished = allEnded
            };
        }

        /// <summary>Two prefixes that leave the same meters on the first training attempts are the same position: keep one.</summary>
        private static int Signature(Entry entry)
        {
            var hash = new HashCode();
            foreach (var engine in entry.Engines.Take(6))
            {
                hash.Add(engine.Status);
                hash.Add(engine.Turn);
                foreach (var meter in engine.DescribeMeters())
                    hash.Add(meter.Value);

                hash.Add(engine.NextOpportunity?.Id);
            }

            return hash.ToHashCode();
        }

        /// <summary>Every cycle of up to <see cref="FixedRotationSolverOptions.CycleLength"/> actions, repeated to the longest rotation, screened on a few attempts; the best few.</summary>
        private List<List<string>> Cycles(Func<IMinigameRandom, ICraftMinigameEngine> factory, List<string> actions, int seed, int[] stream)
        {
            var scored = new List<(List<string> Sequence, double Score)>();
            for (var length = 1; length <= _options.CycleLength; length++)
            {
                foreach (var cycle in Product(actions, length))
                {
                    var sequence = new List<string>(_maximumLength);
                    while (sequence.Count < _maximumLength)
                        sequence.AddRange(cycle);

                    sequence.RemoveRange(_maximumLength, sequence.Count - _maximumLength);
                    var screening = Math.Min(_options.ScreeningRuns, _options.TrainingRuns);
                    scored.Add((sequence, Score(factory, sequence, seed, SimStreams.SolverTraining, stream, screening, _actionLimit)));
                }
            }

            return scored
                .OrderByDescending(item => item.Score)
                .Take(Math.Max(_options.PolishStarts, _options.Finalists) * 2)
                .Select(item => item.Sequence)
                .ToList();
        }

        private static IEnumerable<List<string>> Product(List<string> actions, int length)
        {
            var indexes = new int[length];
            while (true)
            {
                yield return indexes.Select(index => actions[index]).ToList();

                var position = length - 1;
                while (position >= 0 && ++indexes[position] == actions.Count)
                    indexes[position--] = 0;

                if (position < 0)
                    yield break;
            }
        }

        /// <summary>
        /// Local search: at each position keep the best single replacement, swap, deletion or insertion that raises the training score; repeat
        /// for the passes, each scoring on its own attempts (<paramref name="trainFor"/> gives the scorer for a round).
        /// </summary>
        internal List<string> Polish(List<string> start, List<string> actions, Func<int, Func<IReadOnlyList<string>, double>> trainFor)
        {
            var best = start;

            for (var pass = 0; pass < _options.PolishPasses; pass++)
            {
                var train = trainFor(pass);
                var bestScore = train(best);
                var improved = false;
                for (var position = 0; position <= best.Count; position++)
                {
                    List<string> positionBest = null;
                    var positionScore = bestScore;
                    foreach (var variant in Variants(best, position, actions))
                    {
                        if (variant.Count == 0 || variant.Count > _maximumLength)
                            continue;

                        var score = train(variant);
                        if (score > positionScore + Improvement)
                        {
                            positionBest = variant;
                            positionScore = score;
                        }
                    }

                    if (positionBest == null)
                        continue;

                    best = positionBest;
                    bestScore = positionScore;
                    improved = true;
                }

                if (!improved)
                    break;
            }

            return best;
        }

        private static IEnumerable<List<string>> Variants(List<string> sequence, int position, List<string> actions)
        {
            foreach (var action in actions)
            {
                if (position < sequence.Count && sequence[position] != action)
                {
                    var replaced = new List<string>(sequence);
                    replaced[position] = action;
                    yield return replaced;
                }

                var inserted = new List<string>(sequence);
                inserted.Insert(position, action);
                yield return inserted;

                // A pair at once reaches rotations that only pay off with two of the same action in a row.
                var insertedPair = new List<string>(sequence);
                insertedPair.InsertRange(position, new[] { action, action });
                yield return insertedPair;
            }

            if (position < sequence.Count)
            {
                var deleted = new List<string>(sequence);
                deleted.RemoveAt(position);
                yield return deleted;
            }

            if (position + 1 < sequence.Count)
            {
                var deletedPair = new List<string>(sequence);
                deletedPair.RemoveRange(position, 2);
                yield return deletedPair;
            }

            if (position + 1 < sequence.Count && sequence[position] != sequence[position + 1])
            {
                var swapped = new List<string>(sequence);
                (swapped[position], swapped[position + 1]) = (swapped[position + 1], swapped[position]);
                yield return swapped;
            }
        }

        private static List<List<string>> Distinct(IEnumerable<List<string>> sequences)
        {
            var seen = new HashSet<string>();
            return sequences.Where(sequence => sequence.Count > 0 && seen.Add(Key(sequence))).ToList();
        }

        private static string Key(IReadOnlyList<string> sequence) => string.Join(">", sequence);

        private static int[] Stream(int kind, int[] stream, int step, int run)
        {
            var parts = new int[stream.Length + 3];
            parts[0] = kind;
            stream.CopyTo(parts, 1);
            parts[^2] = step;
            parts[^1] = run;
            return parts;
        }
    }
}
