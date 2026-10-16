using Newtonsoft.Json;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PvpCombatService;

namespace SWLOR.CombatSim.Rules
{
    /// <summary>
    /// Rule presets: the rules before the combat tuning (the October 2026 PvP test the sample scenarios are calibrated
    /// against), and the game as this code builds it (healing, FP costs, revives and fixes for PvE and PvP, and the
    /// PvP rules: a healing ramp that stops at -60% and a damage ramp up to double).
    /// </summary>
    public static class RuleSets
    {
        public const string PreTuningName = "pre-tuning";
        public const string CurrentName = "current";

        public static IReadOnlyList<string> Names { get; } = new[] { PreTuningName, CurrentName };

        /// <summary>Reads a rule set from JSON, such as an edited copy of a preset; a file without a name is named after the file.</summary>
        public static RuleSet Load(string path)
        {
            RuleSet rules;
            try
            {
                // Unknown names fail, so a misspelled setting can't silently keep its default.
                rules = JsonConvert.DeserializeObject<RuleSet>(File.ReadAllText(path), new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error });
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException($"Rule set file '{path}': {ex.Message}", ex);
            }

            if (rules == null)
                throw new InvalidDataException($"Rule set file '{path}' is empty.");
            var problems = rules.GetProblems();
            if (problems.Count > 0)
                throw new InvalidDataException($"Rule set file '{path}': {string.Join("; ", problems)}.");
            if (string.IsNullOrWhiteSpace(rules.Name))
                rules.Name = Path.GetFileNameWithoutExtension(path);

            return rules;
        }

        public static RuleSet Get(string name)
        {
            return name.ToLowerInvariant() switch
            {
                PreTuningName => PreTuning(),
                CurrentName => Current(),
                _ => throw new ArgumentException($"Unknown rule set '{name}'. Known: {string.Join(", ", Names)}.")
            };
        }

        /// <summary>
        /// The game data now holds the tuned recasts and FP costs, so this puts back the old ones: Benevolence and
        /// Med Kit every 6 seconds, and FP costs at skill 28+ divided by 1.2, which exactly undoes the rounded +20%.
        /// </summary>
        public static RuleSet PreTuning() => new()
        {
            Name = PreTuningName,
            RecastOverrides = new Dictionary<string, float>
            {
                ["Benevolence"] = 6f,
                ["MedKit"] = 6f
            },
            FpCostMultiplier = 1f / 1.2f
        };

        public static RuleSet Current() => new()
        {
            Name = CurrentName,
            ForceAffinityHealingCap = 1.25f,
            TemporaryHpCapPercent = 50f,
            ConvergenceDurationSeconds = 20f,
            HarmonyInstantHealsOnly = true,
            HarmonyCooldownSeconds = 6f,
            RefuseHitPointCostsAtOneHitPoint = true,
            ScaleHitPointSpendRestoreToHitPointsLost = true,
            HostileAbilityDelaySeconds = HostileAbilityDelay.Seconds,
            Revives = ReviveRule.BlockedInPvpLimitedInPve,
            // The PvP rules come from the game itself, so the two can't drift apart.
            PvpHealingRamp = ToRamp(PvpCombatRules.HealingReductionSteps),
            PvpDamageBackstop = ToRamp(PvpCombatRules.DamageIncreaseSteps),
            PvpCombatWindowSeconds = PvpCombatRules.CombatWindowSeconds
        };

        private static List<RampStep> ToRamp(IReadOnlyList<PvpCombatRampStep> steps) =>
            steps.Select(step => new RampStep(step.AtSeconds, step.Percent)).ToList();
    }
}
