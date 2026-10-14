using SWLOR.CombatSim.Data;
using SWLOR.Game.Server.Service.AbilityService;

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

        /// <summary>HP-cost abilities are refused at 1 HP instead of landing without paying.</summary>
        public bool RefuseHitPointCostsAtOneHitPoint { get; set; }

        /// <summary>Bloodlust's stamina restore shrinks with the HP actually lost, instead of paying in full.</summary>
        public bool ScaleHitPointSpendRestoreToHitPointsLost { get; set; }

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

        /// <summary>Values the simulation can't run with, such as a negative cost multiplier; empty when the rule set is usable.</summary>
        public List<string> GetProblems()
        {
            var problems = new List<string>();

            void AtLeast(string name, double value, double minimum)
            {
                if (!double.IsFinite(value) || value < minimum)
                    problems.Add($"{name} must be a number of at least {minimum}");
            }

            void Between(string name, double value, double minimum, double maximum)
            {
                if (!double.IsFinite(value) || value < minimum || value > maximum)
                    problems.Add($"{name} must be a number from {minimum} to {maximum}");
            }

            void Ramp(string name, List<RampStep> steps, double? maximumPercent)
            {
                if (steps == null)
                {
                    problems.Add($"{name} must be a list; use [] for none");
                    return;
                }

                for (var index = 0; index < steps.Count; index++)
                {
                    if (steps[index] == null)
                    {
                        problems.Add($"{name}[{index}] is null");
                        continue;
                    }

                    AtLeast($"{name}[{index}].AtSeconds", steps[index].AtSeconds, 0);
                    if (maximumPercent.HasValue)
                        Between($"{name}[{index}].Percent", steps[index].Percent, 0, maximumPercent.Value);
                    else
                        AtLeast($"{name}[{index}].Percent", steps[index].Percent, 0);
                }
            }

            // Below 1 the cap would also cut heals from casters with no affinity at all.
            AtLeast(nameof(ForceAffinityHealingCap), ForceAffinityHealingCap, 1);

            if (RecastOverrides == null)
                problems.Add($"{nameof(RecastOverrides)} must be an object; use {{}} for none");
            else
                foreach (var (group, seconds) in RecastOverrides)
                {
                    // Keys must match the game's recast group names exactly, or the override would never apply.
                    if (!Enum.GetNames<RecastGroup>().Contains(group))
                        problems.Add($"{nameof(RecastOverrides)} names an unknown recast group '{group}'");
                    AtLeast($"{nameof(RecastOverrides)}[{group}]", seconds, 0);
                }

            if (TemporaryHpCapPercent.HasValue)
                AtLeast(nameof(TemporaryHpCapPercent), TemporaryHpCapPercent.Value, 0);

            AtLeast(nameof(ConvergenceDurationSeconds), ConvergenceDurationSeconds, 0);
            AtLeast(nameof(HarmonyCooldownSeconds), HarmonyCooldownSeconds, 0);
            AtLeast(nameof(CostSkillThreshold), CostSkillThreshold, 0);
            AtLeast(nameof(FpCostMultiplier), FpCostMultiplier, 0);
            AtLeast(nameof(StmCostMultiplier), StmCostMultiplier, 0);

            if (!Enum.IsDefined(Revives))
                problems.Add($"{nameof(Revives)} must be one of {string.Join(", ", Enum.GetNames<ReviveRule>())}");

            AtLeast(nameof(PveReviveLockoutSeconds), PveReviveLockoutSeconds, 0);
            AtLeast(nameof(PvpReviveDamageWindowSeconds), PvpReviveDamageWindowSeconds, 0);
            AtLeast(nameof(PvpHealingMultiplier), PvpHealingMultiplier, 0);
            AtLeast(nameof(PvpTemporaryHpMultiplier), PvpTemporaryHpMultiplier, 0);
            if (!double.IsFinite(PvpCombatWindowSeconds) || PvpCombatWindowSeconds <= 0)
                problems.Add($"{nameof(PvpCombatWindowSeconds)} must be a number above 0");

            // The healing ramp is a reduction, so past 100% healing would turn into damage.
            Ramp(nameof(PvpHealingRamp), PvpHealingRamp, 100);
            Ramp(nameof(PvpDamageBackstop), PvpDamageBackstop, null);

            return problems;
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
