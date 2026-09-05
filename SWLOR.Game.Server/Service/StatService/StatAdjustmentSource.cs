using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.StatService
{
    /// <summary>
    /// Keeps a stat payload with its own conditions, duration and limits. Combining those
    /// parameters across unrelated sources can change when either payload takes effect.
    /// </summary>
    public sealed class StatAdjustmentSource
    {
        public string Key { get; }
        private readonly IReadOnlyDictionary<StatType, int> _stats;

        public StatAdjustmentSource(string key, IReadOnlyDictionary<StatType, int> stats)
        {
            Key = key;
            _stats = stats;
        }

        public int this[StatType stat] => _stats.TryGetValue(stat, out var value) ? value : 0;

        public string GetModifierGroup(StatType trigger) => $"{Key}:{(int)trigger}";
    }
}
