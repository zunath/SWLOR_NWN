using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.CraftService
{
    public enum CraftSessionStatus { Active, Succeeded, Failed, Aborted }

    // These values are captured when materials are committed. UI and inventory handling
    // stay outside the rules so simulations can replay the same session.
    public sealed record CraftSession
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public int RulesVersion { get; init; } = 1;
        public CraftProfile Profile { get; init; }
        public CraftTechnique Technique { get; init; }
        public IReadOnlyList<CraftCondition> Conditions { get; init; } = Array.Empty<CraftCondition>();
        public int ConditionIndex { get; init; }
        public IReadOnlyDictionary<StatType, int> CraftingStats { get; init; } = new Dictionary<StatType, int>();
        public IReadOnlyDictionary<StatType, int> TriggerCounts { get; init; } = new Dictionary<StatType, int>();
        public IReadOnlyDictionary<CraftBuffType, CraftBuff> Buffs { get; init; } = new Dictionary<CraftBuffType, CraftBuff>();
        public IReadOnlyDictionary<CraftBuffType, int> SupportUses { get; init; } = new Dictionary<CraftBuffType, int>();
        public CraftWorkKind LastWork { get; init; }
        public bool LastWorkSucceeded { get; init; }
        public CraftCondition CurrentCondition => Conditions.Count > ConditionIndex ? Conditions[ConditionIndex] : CraftCondition.Normal;
        public int CraftingStat(StatType stat) => CraftingStats.TryGetValue(stat, out var value) ? Math.Max(0, value) : 0;
        public int TriggerCount(StatType stat) => TriggerCounts.TryGetValue(stat, out var value) ? value : 0;
        public CraftBuff Buff(CraftBuffType type) => Buffs.TryGetValue(type, out var value) ? value : null;
        public int Used(CraftBuffType type) => SupportUses.TryGetValue(type, out var value) ? value : 0;
        public int ForecastLength => Math.Max(1, CraftingStat(StatType.CraftingForecastLength));
        public string Forecast => string.Join(" > ", Conditions.Skip(ConditionIndex + 1).Take(ForecastLength));
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

        public static CraftSession Create(
            int skillRank, int recipeLevel, RecipeLevelDetail targets, int craftsmanship, int control,
            int equipmentCP, CraftProfile profile, CraftTechnique technique, IReadOnlyDictionary<StatType, int> stats,
            Func<int, int> randomIndex, int enhancementPenalty = 0)
        {
            if (profile == CraftProfile.Legacy) return CreateLegacy(skillRank, recipeLevel, targets, craftsmanship, control, equipmentCP, enhancementPenalty);
            if (!Enum.IsDefined(typeof(CraftProfile), profile)) throw new ArgumentOutOfRangeException(nameof(profile));
            var legacy = CreateLegacy(skillRank, recipeLevel, targets, craftsmanship, control, equipmentCP, enhancementPenalty);
            // Profile budgets offset their permanent work efficiency, before opportunity cards and perks.
            return legacy with
            {
                RulesVersion = 2, Profile = profile, Technique = technique,
                MaxQuality = (int)Math.Ceiling(legacy.MaxQuality * (profile == CraftProfile.Delicate ? 1.1 : profile == CraftProfile.Sturdy ? 0.9 : 1)),
                MaxProgress = (int)Math.Ceiling(legacy.MaxProgress * (profile == CraftProfile.Calibrated ? 1.1 : 1)),
                Conditions = CraftConditionDeck.Create(randomIndex),
                CraftingStats = new System.Collections.ObjectModel.ReadOnlyDictionary<StatType, int>(new Dictionary<StatType, int>(stats))
            };
        }

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
