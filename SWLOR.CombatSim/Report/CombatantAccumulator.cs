using SWLOR.CombatSim.Engine;

namespace SWLOR.CombatSim.Report
{
    /// <summary>
    /// One combatant's totals across a batch, added run by run so finished fights are not kept in memory.
    /// </summary>
    internal sealed class CombatantAccumulator
    {
        private readonly string _name;
        private readonly int _teamIndex;
        private readonly List<double> _firstStarvedTimes = new();
        private readonly Dictionary<string, double> _weaponDiagnostics = new();
        private readonly Dictionary<string, double> _abilityCasts = new();
        private int _runs;
        private double _deaths, _damageDealt, _healingDone, _healingReceived, _overhealing, _damageTakenToHp;
        private double _hitPointsSpent, _absorbed, _fpPerMinute, _stmPerMinute, _fpRefunded, _stmRefunded, _starvedSeconds;

        public CombatantAccumulator(string name, int teamIndex)
        {
            _name = name;
            _teamIndex = teamIndex;
        }

        /// <param name="minutesInFight">
        /// Time from the combatant's join to the run's end, for its resource rates; at zero it never acted, so it spent nothing.
        /// </param>
        public void Add(CombatantStats stats, double minutesInFight, WeaponCombatState weapon = null)
        {
            _runs++;
            _deaths += stats.Deaths;
            _damageDealt += stats.DamageDealt;
            _healingDone += stats.HealingDone;
            _healingReceived += stats.HealingReceived;
            _overhealing += stats.Overhealing;
            _damageTakenToHp += stats.DamageTakenToHp;
            _hitPointsSpent += stats.HitPointsSpent;
            _absorbed += stats.DamageAbsorbedByTemporaryHp;
            if (minutesInFight > 0)
            {
                _fpPerMinute += stats.FpSpent / minutesInFight;
                _stmPerMinute += stats.StmSpent / minutesInFight;
            }
            _fpRefunded += stats.FpRefunded;
            _stmRefunded += stats.StmRefunded;
            _starvedSeconds += stats.StarvedSeconds;
            if (stats.FirstStarvedAt.HasValue)
                _firstStarvedTimes.Add(stats.FirstStarvedAt.Value);
            if (weapon != null)
            {
                foreach (var (key, value) in weapon.Diagnostics)
                    _weaponDiagnostics[key] = _weaponDiagnostics.GetValueOrDefault(key) + value;
                foreach (var (key, value) in weapon.Casts)
                    _abilityCasts[key] = _abilityCasts.GetValueOrDefault(key) + value;
            }
        }

        public CombatantSummary ToSummary() => new()
        {
            Name = _name,
            TeamIndex = _teamIndex,
            AverageDeaths = _deaths / _runs,
            AverageDamageDealt = _damageDealt / _runs,
            AverageHealingDone = _healingDone / _runs,
            AverageHealingReceived = _healingReceived / _runs,
            AverageOverhealing = _overhealing / _runs,
            AverageDamageTakenToHp = _damageTakenToHp / _runs,
            AverageHitPointsSpent = _hitPointsSpent / _runs,
            AverageAbsorbed = _absorbed / _runs,
            AverageFpSpentPerMinute = _fpPerMinute / _runs,
            AverageStmSpentPerMinute = _stmPerMinute / _runs,
            AverageFpRefunded = _fpRefunded / _runs,
            AverageStmRefunded = _stmRefunded / _runs,
            AverageStarvedSeconds = _starvedSeconds / _runs,
            MedianFirstStarvedAt = Percentile.Of(_firstStarvedTimes, 50),
            ShareOfRunsStarved = _firstStarvedTimes.Count / (double)_runs,
            AverageWeaponDiagnostics = _weaponDiagnostics.ToDictionary(x => x.Key, x => x.Value / _runs),
            AverageAbilityCasts = _abilityCasts.ToDictionary(x => x.Key, x => x.Value / _runs)
        };
    }
}
