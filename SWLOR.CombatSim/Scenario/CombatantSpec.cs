namespace SWLOR.CombatSim.Scenario
{
    public sealed class CombatantSpec
    {
        public string Name { get; set; }

        /// <summary>Spawns this many copies, numbered after the name.</summary>
        public int Count { get; set; } = 1;

        public bool IsNpc { get; set; }

        /// <summary>Until this many seconds into the fight the combatant does not act, cannot be targeted by enemies and is not reached by allies' zone heals; it still regenerates.</summary>
        public float JoinAtSeconds { get; set; }

        public int MaxHp { get; set; }
        public float HpRegenPer30Seconds { get; set; }

        public int Willpower { get; set; } = 10;
        public int Might { get; set; } = 10;
        public int Social { get; set; } = 10;

        /// <summary>-10 (Dark) to +10 (Light).</summary>
        public int ForceAffinity { get; set; }

        public float CombatReadinessPercent { get; set; }
        public float OutgoingHealingPercent { get; set; }
        public float HealingReceivedPercent { get; set; }
        public float DamageReductionPercent { get; set; }

        public int MaxFp { get; set; }
        public float FpRegenPer30Seconds { get; set; }
        public int MaxStm { get; set; }
        public float StmRegenPer30Seconds { get; set; }

        /// <summary>
        /// PerkType names, with ":level" for perks with several levels. Their Force Convergence, outgoing and received
        /// healing and heal-triggered stamina bonuses are read from the game's perk definitions and added to the values above.
        /// </summary>
        public List<string> Perks { get; set; } = new();

        /// <summary>Seconds between deliberate actions, standing in for reaction time and animations.</summary>
        public float ReactionSeconds { get; set; } = 1f;

        public AutoAttackSpec AutoAttack { get; set; }
        public List<AbilityUseSpec> Abilities { get; set; } = new();

        public const float MaximumPercentBonus = 1000f;

        public const float MaximumRegenPer30Seconds = 1_000_000f;

        /// <summary>The game's Stat.MaximumCombatReadinessPercent.</summary>
        public const float MaximumCombatReadinessPercent = 15f;

        /// <summary>Numbers outside their documented range; empty when the combatant is usable.</summary>
        public List<string> GetProblems()
        {
            var problems = new List<string>();

            void WholeNumber(string name, float value)
            {
                if (!float.IsFinite(value) || value < 0f || value != MathF.Floor(value))
                    problems.Add($"{name} must be a whole number of at least 0");
            }

            void AtLeastZero(string name, float value)
            {
                if (!float.IsFinite(value) || value < 0f)
                    problems.Add($"{name} must be a number of at least 0");
            }

            // Percent bonuses stay small enough for every adjusted heal to remain a sensible whole number.
            void Percent(string name, float value, float minimum)
            {
                if (!float.IsFinite(value) || value < minimum || value > MaximumPercentBonus)
                    problems.Add($"{name} must be from {minimum} to {MaximumPercentBonus}");
            }

            // The game's HP and FP regeneration are whole points per cycle; only stamina is split per heartbeat.
            WholeNumber(nameof(HpRegenPer30Seconds), HpRegenPer30Seconds);
            WholeNumber(nameof(FpRegenPer30Seconds), FpRegenPer30Seconds);
            AtLeastZero(nameof(StmRegenPer30Seconds), StmRegenPer30Seconds);
            // Regeneration beyond any pool is meaningless and would not fit the whole-number amounts it becomes.
            foreach (var (name, value) in new[]
                     {
                         (nameof(HpRegenPer30Seconds), HpRegenPer30Seconds),
                         (nameof(FpRegenPer30Seconds), FpRegenPer30Seconds),
                         (nameof(StmRegenPer30Seconds), StmRegenPer30Seconds)
                     })
            {
                if (value > MaximumRegenPer30Seconds)
                    problems.Add($"{name} must be at most {MaximumRegenPer30Seconds}");
            }
            AtLeastZero(nameof(MaxFp), MaxFp);
            AtLeastZero(nameof(MaxStm), MaxStm);
            if (!float.IsFinite(CombatReadinessPercent) || CombatReadinessPercent < 0f || CombatReadinessPercent > MaximumCombatReadinessPercent)
                problems.Add($"{nameof(CombatReadinessPercent)} must be from 0 to {MaximumCombatReadinessPercent}");
            Percent(nameof(OutgoingHealingPercent), OutgoingHealingPercent, -100f);
            Percent(nameof(HealingReceivedPercent), HealingReceivedPercent, -100f);
            if (ForceAffinity < -10 || ForceAffinity > 10)
                problems.Add($"{nameof(ForceAffinity)} must be from -10 to 10");
            if (!float.IsFinite(DamageReductionPercent) || DamageReductionPercent < 0f || DamageReductionPercent > 100f)
                problems.Add($"{nameof(DamageReductionPercent)} must be from 0 to 100");

            return problems;
        }
    }
}
