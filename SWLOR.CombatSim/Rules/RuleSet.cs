using SWLOR.CombatSim.Data;

namespace SWLOR.CombatSim.Rules
{
    /// <summary>
    /// The balance rules a simulation runs under. Presets live in <see cref="RuleSets"/>; any value can be
    /// overridden from a JSON file to try alternatives without code changes.
    /// </summary>
    public sealed class RuleSet
    {
        public string Name { get; set; }

        /// <summary>Highest multiplier Light affinity may apply to heals and shields (x1.5 today).</summary>
        public float ForceAffinityHealingCap { get; set; } = 1.5f;

        /// <summary>Recast seconds by recast group name, replacing the game data's value.</summary>
        public Dictionary<string, float> RecastOverrides { get; set; } = new();

        /// <summary>Total temporary HP from all sources as a percent of max HP; null means no cap.</summary>
        public float? TemporaryHpCapPercent { get; set; }

        public float ConvergenceDurationSeconds { get; set; } = 30f;
        public bool HarmonyInstantHealsOnly { get; set; }
        public float HarmonyCooldownSeconds { get; set; }

        public int CostSkillThreshold { get; set; } = 28;
        public float FpCostMultiplier { get; set; } = 1f;
        public float StmCostMultiplier { get; set; } = 1f;

        public ReviveRule Revives { get; set; } = ReviveRule.Unrestricted;
        public float PveReviveLockoutSeconds { get; set; } = 300f;
        public float PvpReviveDamageWindowSeconds { get; set; } = 15f;

        public float PvpHealingMultiplier { get; set; } = 1f;
        public float PvpTemporaryHpMultiplier { get; set; } = 1f;
        public List<RampStep> PvpHealingRamp { get; set; } = new();
        public List<RampStep> PvpDamageBackstop { get; set; } = new();
        public float PvpCombatWindowSeconds { get; set; } = 30f;

        public int GetFpCost(AbilityInfo ability) => AdjustCost(ability.FP, ability.SkillRequirement, FpCostMultiplier);
        public int GetStmCost(AbilityInfo ability) => AdjustCost(ability.STM, ability.SkillRequirement, StmCostMultiplier);

        public float GetRecastSeconds(AbilityInfo ability) =>
            RecastOverrides.TryGetValue(ability.RecastGroup, out var seconds) ? seconds : ability.RecastSeconds;

        public int AdjustCost(int baseCost, int skillRequirement, float multiplier)
        {
            if (baseCost <= 0 || skillRequirement < CostSkillThreshold)
                return baseCost;

            return (int)Math.Floor(baseCost * multiplier + 0.5f);
        }

        /// <summary>The percent of the latest step reached, whatever order the steps are listed in.</summary>
        public static float GetStepPercent(IReadOnlyList<RampStep> steps, double seconds)
        {
            var percent = 0f;
            var reachedAt = float.NegativeInfinity;
            foreach (var step in steps)
            {
                if (seconds >= step.AtSeconds && step.AtSeconds >= reachedAt)
                {
                    reachedAt = step.AtSeconds;
                    percent = step.Percent;
                }
            }

            return percent;
        }
    }
}
