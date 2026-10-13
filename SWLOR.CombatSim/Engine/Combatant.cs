using SWLOR.CombatSim.Scenario;

namespace SWLOR.CombatSim.Engine
{
    /// <summary>
    /// A combatant's live state during one simulation run.
    /// </summary>
    public sealed class Combatant
    {
        public const string ForceConvergencePerk = "ForceConvergence";
        public const string DefensiveHarmonyPerk = "DefensiveHarmony";

        public Combatant(string name, int teamIndex, CombatantSpec spec)
        {
            Name = name;
            TeamIndex = teamIndex;
            Spec = spec;
            Hp = spec.MaxHp;
            Fp = spec.MaxFp;
            Stm = spec.MaxStm;
        }

        public string Name { get; }
        public int TeamIndex { get; }
        public CombatantSpec Spec { get; }
        public CombatantStats Stats { get; } = new();

        public bool IsDead { get; set; }
        public double DiedAt { get; set; }
        public double Hp { get; set; }
        public double Fp { get; set; }
        public double Stm { get; set; }

        public List<TemporaryHpPool> TemporaryHp { get; } = new();
        public Dictionary<string, HealOverTimeInstance> HealsOverTime { get; } = new();
        public Dictionary<string, PartyPulseInstance> PartyPulses { get; } = new();
        public Dictionary<string, double> RecastReadyAt { get; } = new();

        /// <summary>When each ability (by feat name) was last used, for <see cref="AbilityUseSpec.MinIntervalSeconds"/>.</summary>
        public Dictionary<string, double> LastUsedAt { get; } = new();

        public PendingCast Casting { get; set; }
        public double NextActionAt { get; set; }
        public double NextAttackAt { get; set; }
        public Combatant AttackTarget { get; set; }
        public double AttackTargetPickedAt { get; set; }
        public bool IsStarved { get; set; }

        public double NextHeartbeatAt { get; set; }
        public int HeartbeatCount { get; set; }
        public double StmRegenCarry { get; set; }

        public double ConvergenceUntil { get; set; } = -1;
        public double ConvergenceNextTickAt { get; set; }
        public double ConvergenceReadyAt { get; set; }
        public double HarmonyReadyAt { get; set; }

        public double LastDamageTakenAt { get; set; } = double.NegativeInfinity;
        public double PvpCombatStartedAt { get; set; } = double.NegativeInfinity;
        public double LastPvpActivityAt { get; set; } = double.NegativeInfinity;
        public double ReviveLockedUntil { get; set; }

        public double TemporaryHpTotal => TemporaryHp.Sum(p => p.Amount);
        public float HpPercent => Spec.MaxHp <= 0 ? 0f : (float)(Hp / Spec.MaxHp * 100.0);
        public bool HasPerk(string perk) => Spec.Perks != null && Spec.Perks.Contains(perk, StringComparer.OrdinalIgnoreCase);
    }
}
