namespace SWLOR.CombatSim.Rules
{
    /// <summary>
    /// Rule presets: today's live rules, Wave 1 (PvE healing, costs and fixes) and Wave 2 (Wave 1 plus PvP rules)
    /// from the October 2026 combat tuning plan.
    /// </summary>
    public static class RuleSets
    {
        public const string CurrentName = "current";
        public const string Wave1Name = "wave1";
        public const string Wave2Name = "wave2";

        public static IReadOnlyList<string> Names { get; } = new[] { CurrentName, Wave1Name, Wave2Name };

        public static RuleSet Get(string name)
        {
            return name.ToLowerInvariant() switch
            {
                CurrentName => Current(),
                Wave1Name => Wave1(),
                Wave2Name => Wave2(),
                _ => throw new ArgumentException($"Unknown rule set '{name}'. Known: {string.Join(", ", Names)}.")
            };
        }

        public static RuleSet Current() => new() { Name = CurrentName };

        public static RuleSet Wave1() => new()
        {
            Name = Wave1Name,
            ForceAffinityHealingCap = 1.25f,
            RecastOverrides = new Dictionary<string, float>
            {
                ["Benevolence"] = 10f,
                ["MedKit"] = 10.5f
            },
            TemporaryHpCapPercent = 50f,
            ConvergenceDurationSeconds = 20f,
            HarmonyInstantHealsOnly = true,
            HarmonyCooldownSeconds = 6f,
            FpCostMultiplier = 1.2f,
            StmCostMultiplier = 1.2f,
            Revives = ReviveRule.BlockedInPvpLimitedInPve
        };

        public static RuleSet Wave2()
        {
            var rules = Wave1();
            rules.Name = Wave2Name;
            rules.PvpHealingRamp = new List<RampStep>
            {
                new(120f, 10f), new(150f, 30f), new(180f, 50f), new(210f, 70f), new(240f, 90f)
            };
            rules.PvpDamageBackstop = new List<RampStep>
            {
                new(240f, 10f), new(270f, 20f), new(300f, 30f), new(330f, 40f), new(360f, 50f)
            };
            return rules;
        }
    }
}
