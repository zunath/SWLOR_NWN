using SWLOR.CraftSim.Rng;

namespace SWLOR.CraftSim.Legacy.Crafting
{
    /// <summary>The rotation players report: Waste Not, Basic Touch repeated, Master's Mend to repair, then synthesis to finish.</summary>
    public sealed class DominantRotationPolicy : ILegacyCraftPolicy
    {
        private static readonly LegacyAction[] Syntheses =
            { LegacyAction.CarefulSynthesis, LegacyAction.RapidSynthesis, LegacyAction.BasicSynthesis };

        public string Name => "Dominant rotation";

        public LegacyAction? Choose(in LegacyCraftState state, LegacyCraftContext context, SimRandom rng)
        {
            if (state.IsFinished)
                return null;

            var mend = LegacyActionSpec.Of(LegacyAction.MastersMend);
            var canMend = context.IsUnlocked(LegacyAction.MastersMend);
            var needed = DurabilityToFinish(state, context);

            if (context.IsUnlocked(LegacyAction.BasicTouch) && state.Quality < context.MaxQuality)
            {
                var touch = LegacyActionSpec.Of(LegacyAction.BasicTouch);
                var wasteNot = LegacyActionSpec.Of(LegacyAction.WasteNot);

                if (context.IsUnlocked(LegacyAction.WasteNot) &&
                    state.WasteNotSteps == 0 &&
                    state.Cp >= wasteNot.CpCost + touch.CpCost &&
                    Potential(state.Durability - touch.DurabilityLoss / 2, state.Cp - wasteNot.CpCost - touch.CpCost, canMend) >= needed)
                {
                    return LegacyAction.WasteNot;
                }

                var loss = LegacyCraftEngine.DurabilityLoss(state, LegacyAction.BasicTouch);
                if (state.Cp >= touch.CpCost)
                {
                    if (state.Durability > loss &&
                        Potential(state.Durability - loss, state.Cp - touch.CpCost, canMend) >= needed)
                    {
                        return LegacyAction.BasicTouch;
                    }

                    if (state.Durability <= loss &&
                        canMend &&
                        state.Cp >= mend.CpCost + touch.CpCost &&
                        Potential(state.Durability + mend.Amount - loss, state.Cp - mend.CpCost - touch.CpCost, canMend) >= needed)
                    {
                        return LegacyAction.MastersMend;
                    }
                }
            }

            var synthesis = BestSynthesis(state, context, canMend);
            if (canMend && state.Cp >= mend.CpCost && state.Durability <= LegacyCraftEngine.DurabilityLoss(state, synthesis))
                return LegacyAction.MastersMend;

            return synthesis;
        }

        /// <summary>The durability on hand plus what the remaining CP could repair.</summary>
        private static int Potential(int durability, int cp, bool canMend)
        {
            var repair = LegacyActionSpec.Of(LegacyAction.MastersMend);
            return durability + (canMend && cp > 0 ? cp / repair.CpCost * repair.Amount : 0);
        }

        /// <summary>The durability the finish needs: Basic Synthesis at its success chance, plus one spare attempt.</summary>
        private static int DurabilityToFinish(in LegacyCraftState state, LegacyCraftContext context)
        {
            var remaining = context.MaxProgress - state.Progress;
            var basic = LegacyActionSpec.Of(LegacyAction.BasicSynthesis);
            var perAttempt = Math.Max(1, context.ProgressOf(LegacyAction.BasicSynthesis)) * basic.BaseChance / 100.0;
            var attempts = Math.Max(1, (int)Math.Ceiling(remaining / perAttempt)) + 1;
            return (attempts - 1) * basic.DurabilityLoss + 1;
        }

        /// <summary>The affordable synthesis with the most expected progress per attempt, keeping one repair affordable.</summary>
        private static LegacyAction BestSynthesis(in LegacyCraftState state, LegacyCraftContext context, bool canMend)
        {
            var repairCp = canMend ? LegacyActionSpec.Of(LegacyAction.MastersMend).CpCost : 0;
            var best = LegacyAction.BasicSynthesis;
            var bestValue = double.MinValue;
            foreach (var action in Syntheses)
            {
                if (!LegacyCraftEngine.CanUse(state, context, action) ||
                    (action != LegacyAction.BasicSynthesis && state.Cp - LegacyCraftEngine.CpCost(state, action) < repairCp))
                {
                    continue;
                }

                var value = context.ProgressOf(action) * LegacyCraftEngine.ChancePercent(state, action);
                if (value > bestValue)
                {
                    bestValue = value;
                    best = action;
                }
            }

            return best;
        }
    }
}
