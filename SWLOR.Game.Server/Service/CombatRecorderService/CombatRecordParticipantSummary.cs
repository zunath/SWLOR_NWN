using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CombatRecorderService
{
    /// <summary>
    /// Per-participant totals of one recording. Dictionary keys are source labels from
    /// <see cref="CombatRecordSourceLabel"/>.
    /// </summary>
    public sealed class CombatRecordParticipantSummary
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsPlayer { get; set; }
        public string Resref { get; set; }

        public Dictionary<string, int> AbilityUses { get; } = new();

        /// <summary>Refused activations: ability, then the refusal shown, then the count.</summary>
        public Dictionary<string, Dictionary<string, int>> AbilityDenials { get; } = new();

        /// <summary>Interrupted casts: ability, then the cause, then the count.</summary>
        public Dictionary<string, Dictionary<string, int>> AbilityInterruptions { get; } = new();
        public Dictionary<string, CombatRecordDamageTotals> DamageDealt { get; } = new();
        public Dictionary<string, CombatRecordDamageTotals> DamageTaken { get; } = new();
        public Dictionary<string, CombatRecordHealingTotals> HealingDone { get; } = new();
        public Dictionary<string, CombatRecordHealingTotals> HealingReceived { get; } = new();
        public Dictionary<string, CombatRecordAmountTotals> TemporaryHitPointsGranted { get; } = new();
        public Dictionary<string, CombatRecordAmountTotals> TemporaryHitPointsReceived { get; } = new();

        /// <summary>Temporary HP this participant's pools absorbed, keyed by the pool's granting source.</summary>
        public Dictionary<string, CombatRecordAmountTotals> TemporaryHitPointsAbsorbed { get; } = new();

        /// <summary>Temporary HP this participant granted that later absorbed damage.</summary>
        public Dictionary<string, CombatRecordAmountTotals> TemporaryHitPointsGrantedAbsorbed { get; } = new();

        public Dictionary<string, CombatRecordAmountTotals> FPSpent { get; } = new();
        public Dictionary<string, CombatRecordAmountTotals> FPRegained { get; } = new();
        public Dictionary<string, CombatRecordAmountTotals> StaminaSpent { get; } = new();
        public Dictionary<string, CombatRecordAmountTotals> StaminaRegained { get; } = new();
        public Dictionary<string, CombatRecordAmountTotals> HitPointsSpent { get; } = new();
        public Dictionary<string, CombatRecordAmountTotals> ItemsConsumed { get; } = new();

        public int Deaths { get; set; }
        public int Kills { get; set; }
        public int Subdued { get; set; }
        public int RevivesReceived { get; set; }
        public int RevivesPerformed { get; set; }
        public double SecondsObserved { get; set; }
        public double SecondsInCombat { get; set; }
    }
}
