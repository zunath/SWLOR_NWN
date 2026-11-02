namespace SWLOR.CraftSim.Legacy.Crafting
{
    /// <summary>One craft action's real costs and effects (CraftViewModel.cs:1727-1864, unlock ranks :39-48).</summary>
    public sealed class LegacyActionSpec
    {
        private LegacyActionSpec(
            LegacyAction action,
            string name,
            int unlockRank,
            int baseChance,
            LegacyChanceBoost boost,
            int cpCost,
            int labelledCpCost,
            int durabilityLoss,
            LegacyActionKind kind,
            int amount)
        {
            Action = action;
            Name = name;
            UnlockRank = unlockRank;
            BaseChance = baseChance;
            Boost = boost;
            CpCost = cpCost;
            LabelledCpCost = labelledCpCost;
            DurabilityLoss = durabilityLoss;
            Kind = kind;
            Amount = amount;
        }

        public LegacyAction Action { get; }
        public string Name { get; }
        public int UnlockRank { get; }

        /// <summary>Success chance in percent when no Steady Hand or Muscle Memory is active.</summary>
        public int BaseChance { get; }

        public LegacyChanceBoost Boost { get; }

        /// <summary>CP actually spent, before Veneration halves a Rapid or Careful Synthesis.</summary>
        public int CpCost { get; }

        /// <summary>The CP the window's button shows. Careful Synthesis and Precise Touch show 15 but spend 10.</summary>
        public int LabelledCpCost { get; }

        public int DurabilityLoss { get; }
        public LegacyActionKind Kind { get; }

        /// <summary>Base progress or quality, or the durability a Master's Mend restores.</summary>
        public int Amount { get; }

        /// <summary>Veneration halves the CP of the next four Rapid or Careful Syntheses.</summary>
        public bool HalvedByVeneration => Action is LegacyAction.RapidSynthesis or LegacyAction.CarefulSynthesis;

        public static readonly IReadOnlyList<LegacyActionSpec> All = new[]
        {
            new LegacyActionSpec(LegacyAction.BasicSynthesis, "Basic Synthesis", 0, 90, LegacyChanceBoost.SteadyHand, 0, 0, 10, LegacyActionKind.Progress, 10),
            new LegacyActionSpec(LegacyAction.RapidSynthesis, "Rapid Synthesis", 10, 75, LegacyChanceBoost.SteadyHand, 6, 6, 10, LegacyActionKind.Progress, 30),
            new LegacyActionSpec(LegacyAction.CarefulSynthesis, "Careful Synthesis", 30, 50, LegacyChanceBoost.SteadyHand, 10, 15, 10, LegacyActionKind.Progress, 80),
            new LegacyActionSpec(LegacyAction.BasicTouch, "Basic Touch", 5, 90, LegacyChanceBoost.MuscleMemory, 3, 3, 10, LegacyActionKind.Quality, 10),
            new LegacyActionSpec(LegacyAction.StandardTouch, "Standard Touch", 15, 75, LegacyChanceBoost.MuscleMemory, 6, 6, 10, LegacyActionKind.Quality, 30),
            new LegacyActionSpec(LegacyAction.PreciseTouch, "Precise Touch", 35, 50, LegacyChanceBoost.MuscleMemory, 10, 15, 10, LegacyActionKind.Quality, 80),
            new LegacyActionSpec(LegacyAction.MastersMend, "Master's Mend", 10, 100, LegacyChanceBoost.None, 10, 10, 0, LegacyActionKind.Repair, 30),
            new LegacyActionSpec(LegacyAction.SteadyHand, "Steady Hand", 20, 100, LegacyChanceBoost.None, 12, 12, 0, LegacyActionKind.Buff, 0),
            new LegacyActionSpec(LegacyAction.MuscleMemory, "Muscle Memory", 40, 100, LegacyChanceBoost.None, 12, 12, 0, LegacyActionKind.Buff, 0),
            new LegacyActionSpec(LegacyAction.Veneration, "Veneration", 25, 100, LegacyChanceBoost.None, 8, 8, 10, LegacyActionKind.Buff, 4),
            new LegacyActionSpec(LegacyAction.WasteNot, "Waste Not", 8, 100, LegacyChanceBoost.None, 4, 4, 0, LegacyActionKind.Buff, 4),
        };

        private static readonly LegacyActionSpec[] ByAction = BuildIndex();

        public static LegacyActionSpec Of(LegacyAction action) => ByAction[(int)action];

        private static LegacyActionSpec[] BuildIndex()
        {
            var index = new LegacyActionSpec[All.Count];
            foreach (var spec in All)
                index[(int)spec.Action] = spec;

            return index;
        }
    }
}
