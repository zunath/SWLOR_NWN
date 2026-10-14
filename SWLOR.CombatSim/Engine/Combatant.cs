using SWLOR.CombatSim.Scenario;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.CombatSim.Engine
{
    /// <summary>
    /// A combatant's live state during one simulation run.
    /// </summary>
    public sealed class Combatant
    {
        private readonly IReadOnlyDictionary<StatType, int> _perkStats;

        public Combatant(string name, int teamIndex, CombatantSpec spec, IReadOnlyDictionary<StatType, int> perkStats = null)
        {
            Name = name;
            TeamIndex = teamIndex;
            Spec = spec;
            _perkStats = perkStats ?? new Dictionary<StatType, int>();
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
        /// <summary>Zone schedules, one per cast; a zone with a damage reduction has a second entry that refreshes it.</summary>
        public List<PartyPulseInstance> PartyPulses { get; } = new();
        public Dictionary<string, DamageReductionInstance> DamageReductions { get; } = new();
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
        public double ConvergenceAccuracyUntil { get; set; } = -1;
        public double HarmonyReadyAt { get; set; }
        public double HitPointSpendRestoreReadyAt { get; set; }
        public QueuedWeaponAbility QueuedWeaponAbility { get; set; }

        /// <summary>The last death came from a player in a PvP fight, which holds revives for the party's damage window.</summary>
        public bool KilledByPlayer { get; set; }
        public double NextBlockedReviveCountAt { get; set; }

        public double LastDamageTakenAt { get; set; } = double.NegativeInfinity;
        public double PvpCombatStartedAt { get; set; } = double.NegativeInfinity;
        public double LastPvpActivityAt { get; set; } = double.NegativeInfinity;
        public double ReviveLockedUntil { get; set; }

        public double TemporaryHpTotal => TemporaryHp.Sum(p => p.Amount);
        public float HpPercent => Spec.MaxHp <= 0 ? 0f : (float)(Hp / Spec.MaxHp * 100.0);

        /// <summary>HP percent counting temporary HP, as the game's current HP does.</summary>
        public float HpPercentWithTemporaryHp => Spec.MaxHp <= 0 ? 0f : (float)((Hp + TemporaryHpTotal) / Spec.MaxHp * 100.0);

        public float OutgoingHealingPercent => Spec.OutgoingHealingPercent + GetStat(StatType.OutgoingAbilityHealingPercentAdjustment);
        public float HealingReceivedPercent => Spec.HealingReceivedPercent + GetStat(StatType.HealingReceivedPercentAdjustment);

        /// <summary>The combined bonus the combatant's perks give to a stat the simulator reads.</summary>
        public int GetStat(StatType stat) => _perkStats.TryGetValue(stat, out var value) ? value : 0;

        /// <summary>The most recent healing schedule of the caster's zone with this key, or null when none is active.</summary>
        public PartyPulseInstance GetZone(string key) =>
            PartyPulses.LastOrDefault(pulse => !pulse.RefreshesDamageReduction && pulse.Effect.Key == key);
    }
}
