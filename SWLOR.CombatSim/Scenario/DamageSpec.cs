namespace SWLOR.CombatSim.Scenario
{
    /// <summary>
    /// Damage a hostile ability or autoattack deals, taken from combat logs or set by hand.
    /// </summary>
    public sealed class DamageSpec
    {
        public float Min { get; set; }
        public float Max { get; set; }

        /// <summary>Chance to land, 0 to 1.</summary>
        public float HitChance { get; set; } = 1f;

        /// <summary>Hit chance against specific targets by name, overriding <see cref="HitChance"/>.</summary>
        public Dictionary<string, float> HitChanceVs { get; set; } = new();

        public float CritChance { get; set; }
        public float CritMultiplier { get; set; } = 1.5f;
        public int Hits { get; set; } = 1;

        /// <summary>Hits every living enemy instead of one target.</summary>
        public bool Area { get; set; }

        /// <summary>
        /// Distinct enemies one use reaches: the chosen target plus random others, each taking <see cref="Hits"/> rolls.
        /// Stands in for an area that catches a few enemies; ignored when <see cref="Area"/> is set.
        /// </summary>
        public int Targets { get; set; } = 1;

        /// <summary>Damage percent for each of <see cref="Targets"/> after the chosen one, such as Force Lightning's half-damage arcs.</summary>
        public float ExtraTargetDamagePercent { get; set; } = 100f;

        /// <summary>Decides which damage reductions apply; weapon and autoattack damage is physical.</summary>
        public DamageType Type { get; set; } = DamageType.Physical;

        public float GetHitChance(string targetName) =>
            HitChanceVs != null && HitChanceVs.TryGetValue(targetName, out var chance) ? chance : HitChance;

        /// <summary>Values outside their documented range; empty when the profile is usable.</summary>
        public List<string> GetProblems()
        {
            var problems = new List<string>();

            void Chance(string name, float value)
            {
                if (!float.IsFinite(value) || value < 0f || value > 1f)
                    problems.Add($"{name} must be from 0 to 1");
            }

            if (!float.IsFinite(Min) || Min < 0f)
                problems.Add("Min must be a number of at least 0");
            if (!float.IsFinite(Max) || Max < Min)
                problems.Add("Max must be a number of at least Min");

            Chance(nameof(HitChance), HitChance);
            if (HitChanceVs != null)
                foreach (var (target, chance) in HitChanceVs)
                    Chance($"{nameof(HitChanceVs)}[{target}]", chance);

            Chance(nameof(CritChance), CritChance);
            if (!float.IsFinite(CritMultiplier) || CritMultiplier < 1f)
                problems.Add("CritMultiplier must be a number of at least 1");
            if (Hits < 1)
                problems.Add("Hits must be at least 1");
            if (Targets < 1)
                problems.Add("Targets must be at least 1");
            if (!float.IsFinite(ExtraTargetDamagePercent) || ExtraTargetDamagePercent < 0f)
                problems.Add("ExtraTargetDamagePercent must be a number of at least 0");
            if (!Enum.IsDefined(Type))
                problems.Add($"Type must be one of {string.Join(", ", Enum.GetNames<DamageType>())}");

            return problems;
        }
    }
}
