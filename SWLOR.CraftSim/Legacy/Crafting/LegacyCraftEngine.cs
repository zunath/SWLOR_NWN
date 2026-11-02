using SWLOR.CraftSim.Rng;

namespace SWLOR.CraftSim.Legacy.Crafting
{
    /// <summary>The old minigame's action resolution (CraftViewModel.cs:1672-1725, actions CraftViewModel.cs:1727-1864).</summary>
    public static class LegacyCraftEngine
    {
        public const int VenerationSteps = 4;
        public const int WasteNotSteps = 4;

        /// <summary>The CP the action would spend now: Veneration halves a Rapid or Careful Synthesis.</summary>
        public static int CpCost(in LegacyCraftState state, LegacyAction action)
        {
            var spec = LegacyActionSpec.Of(action);
            return spec.HalvedByVeneration && state.VenerationSteps > 0 ? spec.CpCost / 2 : spec.CpCost;
        }

        /// <summary>The action's success chance in percent now.</summary>
        public static int ChancePercent(in LegacyCraftState state, LegacyAction action)
        {
            var spec = LegacyActionSpec.Of(action);
            var boosted = spec.Boost switch
            {
                LegacyChanceBoost.SteadyHand => state.SteadyHandActive,
                LegacyChanceBoost.MuscleMemory => state.MuscleMemoryActive,
                _ => false
            };

            return boosted ? 100 : spec.BaseChance;
        }

        /// <summary>The durability the action would lose now: Waste Not halves it.</summary>
        public static int DurabilityLoss(in LegacyCraftState state, LegacyAction action)
        {
            var loss = LegacyActionSpec.Of(action).DurabilityLoss;
            return loss > 0 && state.WasteNotSteps > 0 ? loss / 2 : loss;
        }

        public static bool CanUse(in LegacyCraftState state, LegacyCraftContext context, LegacyAction action) =>
            !state.IsFinished && context.IsUnlocked(action) && state.Cp >= CpCost(state, action);

        public static LegacyActionResult Apply(ref LegacyCraftState state, LegacyCraftContext context, LegacyAction action, SimRandom rng)
        {
            if (state.IsFinished || !context.IsUnlocked(action))
                return LegacyActionResult.Refused;

            var spec = LegacyActionSpec.Of(action);
            var chance = ChancePercent(state, action);
            var cpCost = spec.CpCost;
            if (spec.HalvedByVeneration && state.VenerationSteps > 0)
            {
                state.VenerationSteps--;
                cpCost /= 2;
            }

            if (state.Cp < cpCost)
                return LegacyActionResult.Refused;

            var durabilityLoss = spec.DurabilityLoss;
            if (durabilityLoss > 0)
            {
                if (state.WasteNotSteps > 0)
                {
                    state.WasteNotSteps--;
                    durabilityLoss /= 2;
                }

                state.Durability -= durabilityLoss;
                if (state.Durability < 0)
                    state.Durability = 0;
            }

            state.Cp -= cpCost;
            state.Actions++;

            var rolled = rng.D100() <= chance;
            if (rolled)
                ApplyEffect(ref state, context, spec);

            if (state.Progress >= context.MaxProgress)
            {
                state.Progress = context.MaxProgress;
                state.Outcome = LegacyCraftOutcome.Succeeded;
            }
            else if (state.Durability <= 0)
            {
                state.Outcome = LegacyCraftOutcome.Failed;
            }

            return rolled ? LegacyActionResult.RollSucceeded : LegacyActionResult.RollFailed;
        }

        private static void ApplyEffect(ref LegacyCraftState state, LegacyCraftContext context, LegacyActionSpec spec)
        {
            switch (spec.Action)
            {
                case LegacyAction.BasicSynthesis:
                case LegacyAction.RapidSynthesis:
                case LegacyAction.CarefulSynthesis:
                    state.Progress = Math.Min(state.Progress + context.ProgressOf(spec.Action), context.MaxProgress);
                    state.SteadyHandActive = false;
                    break;
                case LegacyAction.BasicTouch:
                case LegacyAction.StandardTouch:
                case LegacyAction.PreciseTouch:
                    state.Quality = Math.Min(state.Quality + context.QualityOf(spec.Action), context.MaxQuality);
                    state.MuscleMemoryActive = false;
                    break;
                case LegacyAction.MastersMend:
                    state.Durability = Math.Min(state.Durability + spec.Amount, context.MaxDurability);
                    break;
                case LegacyAction.SteadyHand:
                    state.SteadyHandActive = true;
                    break;
                case LegacyAction.MuscleMemory:
                    state.MuscleMemoryActive = true;
                    break;
                case LegacyAction.Veneration:
                    state.VenerationSteps = VenerationSteps;
                    break;
                case LegacyAction.WasteNot:
                    state.WasteNotSteps = WasteNotSteps;
                    break;
            }
        }
    }
}
