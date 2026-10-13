namespace SWLOR.CombatSim.Rules
{
    /// <summary>
    /// Rule presets: today's live rules, the proposed tuning (healing, costs, revives and fixes for PvE and PvP),
    /// and that tuning plus the PvP attrition rules (a healing ramp that stops at -60% and a damage ramp up to double).
    /// </summary>
    public static class RuleSets
    {
        public const string CurrentName = "current";
        public const string TunedName = "tuned";
        public const string TunedPvpName = "tuned-pvp";

        public static IReadOnlyList<string> Names { get; } = new[] { CurrentName, TunedName, TunedPvpName };

        public static RuleSet Get(string name)
        {
            return name.ToLowerInvariant() switch
            {
                CurrentName => Current(),
                TunedName => Tuned(),
                TunedPvpName => TunedPvp(),
                _ => throw new ArgumentException($"Unknown rule set '{name}'. Known: {string.Join(", ", Names)}.")
            };
        }

        public static RuleSet Current() => new() { Name = CurrentName };

        public static RuleSet Tuned() => new()
        {
            Name = TunedName,
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

        public static RuleSet TunedPvp()
        {
            var rules = Tuned();
            rules.Name = TunedPvpName;
            rules.PvpHealingRamp = new List<RampStep>
            {
                new(120f, 10f), new(150f, 20f), new(180f, 30f), new(210f, 45f), new(240f, 60f)
            };
            rules.PvpDamageBackstop = new List<RampStep>
            {
                new(180f, 20f), new(210f, 40f), new(240f, 60f), new(270f, 80f), new(300f, 100f)
            };
            return rules;
        }
    }
}
