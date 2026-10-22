using System.Collections.Concurrent;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Legacy.Crafting
{
    public sealed class LegacyCraftBaselineOptions
    {
        public int Runs { get; init; } = 200;
        public int Seed { get; init; } = 1;

        /// <summary>The policies to play; the first also plays the sensitivity offsets.</summary>
        public IReadOnlyList<ILegacyCraftPolicy> Policies { get; init; } = new ILegacyCraftPolicy[] { new DominantRotationPolicy() };

        /// <summary>Rank differences the first policy also plays besides 0 (rank = recipe level + offset, kept in the recipe's allowed range).</summary>
        public IReadOnlyList<int> SensitivityOffsets { get; init; } = new[] { -LegacyCraftRules.RecipeUnlockOffset, 5 };

        public IReadOnlyList<SkillType> Skills { get; init; } = DefaultSkills;

        public static readonly IReadOnlyList<SkillType> DefaultSkills = new[]
        {
            SkillType.Smithery, SkillType.Engineering, SkillType.Fabrication, SkillType.Agriculture, SkillType.Espionage
        };
    }
}
