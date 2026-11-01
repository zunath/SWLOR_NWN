using SWLOR.CraftSim.Rng;

namespace SWLOR.CraftSim.Legacy.Crafting
{
    public static class LegacyCraftRunner
    {
        /// <summary>No policy needs this many actions; a craft that reaches it counts as failed.</summary>
        public const int ActionLimit = 300;

        public static LegacyCraftResult Play(LegacyCraftContext context, ILegacyCraftPolicy policy, SimRandom rng)
        {
            var state = context.Start();
            for (var step = 0; step < ActionLimit && !state.IsFinished; step++)
            {
                var action = policy.Choose(state, context, rng);
                if (action == null || LegacyCraftEngine.Apply(ref state, context, action.Value, rng) == LegacyActionResult.Refused)
                    break;
            }

            var succeeded = state.Outcome == LegacyCraftOutcome.Succeeded;
            return new LegacyCraftResult(
                succeeded,
                succeeded ? state.Quality / (double)context.MaxQuality : 0,
                state.Actions,
                context.MaxCp - state.Cp,
                succeeded ? LegacyEnhancementRules.TransferChancePercent(state.Quality, context.MaxQuality) : 0);
        }
    }
}
