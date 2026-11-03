using SWLOR.CraftSim.Rng;

namespace SWLOR.CraftSim.Legacy.Crafting
{
    /// <summary>Tries each usable action against sampled futures finished by the dominant rotation, and takes the best mean score.</summary>
    public sealed class RolloutPolicy : ILegacyCraftPolicy
    {
        public const int DefaultRollouts = 8;
        public const int MaximumRollouts = 256;
        private const int RolloutStepLimit = 200;

        private readonly ILegacyCraftPolicy _continuation;
        private readonly int _rollouts;

        public RolloutPolicy(int rollouts = DefaultRollouts, ILegacyCraftPolicy continuation = null)
        {
            if (rollouts < 1 || rollouts > MaximumRollouts)
                throw new ArgumentOutOfRangeException(nameof(rollouts), $"A lookahead samples 1 to {MaximumRollouts} futures.");

            _rollouts = rollouts;
            _continuation = continuation ?? new DominantRotationPolicy();
        }

        public string Name => "Lookahead";

        public LegacyAction? Choose(in LegacyCraftState state, LegacyCraftContext context, SimRandom rng)
        {
            if (state.IsFinished)
                return null;

            Span<ulong> futures = stackalloc ulong[_rollouts];
            for (var index = 0; index < futures.Length; index++)
                futures[index] = rng.NextUInt64();

            LegacyAction? best = null;
            var bestScore = double.MinValue;
            var bestCost = int.MaxValue;
            foreach (var spec in LegacyActionSpec.All)
            {
                var action = spec.Action;
                if (!LegacyCraftEngine.CanUse(state, context, action) || IsNoOp(state, context, action))
                    continue;

                var score = 0.0;
                foreach (var future in futures)
                {
                    var trial = state;
                    var trialRng = new SimRandom(unchecked((long)future));
                    LegacyCraftEngine.Apply(ref trial, context, action, trialRng);
                    score += PlayOut(ref trial, context, trialRng);
                }

                score /= futures.Length;
                var cost = LegacyCraftEngine.CpCost(state, action);
                if (score > bestScore + 1e-9 || (Math.Abs(score - bestScore) <= 1e-9 && cost < bestCost))
                {
                    best = action;
                    bestScore = score;
                    bestCost = cost;
                }
            }

            return best;
        }

        private static bool IsNoOp(in LegacyCraftState state, LegacyCraftContext context, LegacyAction action) => action switch
        {
            LegacyAction.SteadyHand => state.SteadyHandActive,
            LegacyAction.MuscleMemory => state.MuscleMemoryActive,
            LegacyAction.MastersMend => state.Durability >= context.MaxDurability,
            LegacyAction.BasicTouch or LegacyAction.StandardTouch or LegacyAction.PreciseTouch => state.Quality >= context.MaxQuality,
            _ => false
        };

        private double PlayOut(ref LegacyCraftState state, LegacyCraftContext context, SimRandom rng)
        {
            for (var step = 0; step < RolloutStepLimit && !state.IsFinished; step++)
            {
                var next = _continuation.Choose(state, context, rng);
                if (next == null || LegacyCraftEngine.Apply(ref state, context, next.Value, rng) == LegacyActionResult.Refused)
                    return 0;
            }

            return Score(state, context);
        }

        /// <summary>0 for a failed craft, otherwise 0.5 plus half the quality fraction.</summary>
        public static double Score(in LegacyCraftState state, LegacyCraftContext context) =>
            state.Outcome == LegacyCraftOutcome.Succeeded
                ? 0.5 + 0.5 * state.Quality / context.MaxQuality
                : 0;
    }
}
