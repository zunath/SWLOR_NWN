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
    }
}
