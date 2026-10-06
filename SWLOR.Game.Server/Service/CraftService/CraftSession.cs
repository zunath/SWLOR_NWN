namespace SWLOR.Game.Server.Service.CraftService
{
    public enum CraftSessionStatus { Active, Succeeded, Failed, Aborted }

    // These values are captured when materials are committed. UI and inventory handling
    // stay outside the rules so simulations can replay the same session.
    public sealed record CraftSession
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public int RulesVersion { get; init; } = 1;
        public int SkillRank { get; init; }
        public int RecipeLevel { get; init; }
        public int Craftsmanship { get; init; }
        public int Control { get; init; }
        public int MaxCP { get; init; }
        public int CP { get; init; }
        public int MaxDurability { get; init; }
        public int Durability { get; init; }
        public int MaxProgress { get; init; }
        public int Progress { get; init; }
        public int MaxQuality { get; init; }
        public int Quality { get; init; }
        public bool SteadyHandActive { get; init; }
        public bool MuscleMemoryActive { get; init; }
        public int VenerationCharges { get; init; }
        public int WasteNotCharges { get; init; }
        public int ActionCount { get; init; }
        public CraftSessionStatus Status { get; init; }

        public static CraftSession CreateLegacy(
            int skillRank, int recipeLevel, RecipeLevelDetail targets,
            int craftsmanship, int control, int equipmentCP, int enhancementPenalty = 0)
        {
            if (skillRank < 0 || recipeLevel < 0 || enhancementPenalty < 0)
                throw new ArgumentOutOfRangeException(nameof(skillRank));
            if (targets == null || targets.Progress <= 0 || targets.Quality <= 0 || targets.Durability <= 0)
                throw new ArgumentException("Crafting targets must be positive.", nameof(targets));

            var delta = skillRank - recipeLevel;
            // Preserve the legacy target calculation, including its above-level scaling.
            var modifier = delta < 0 ? -delta * 0.25f : Math.Min(delta * 0.05f, 0.25f);
            var maxCP = Math.Max(0, (int)(equipmentCP + skillRank * 0.75f) + (skillRank >= 25 ? 31 : 0));
            return new CraftSession
            {
                SkillRank = skillRank, RecipeLevel = recipeLevel,
                Craftsmanship = craftsmanship, Control = control,
                MaxCP = maxCP, CP = maxCP,
                MaxDurability = targets.Durability, Durability = targets.Durability,
                MaxProgress = (int)(targets.Progress + targets.Progress * modifier) + enhancementPenalty,
                MaxQuality = targets.Quality
            };
        }
    }

    public sealed record CraftActionRequest(Guid SessionId, int ExpectedActionCount, CraftActionType Action);
}
