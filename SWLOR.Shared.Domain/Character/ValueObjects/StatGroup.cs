using SWLOR.Shared.Domain.Character.Enums;

namespace SWLOR.Shared.Domain.Character.ValueObjects
{
    public class StatGroup
    {
        private readonly Dictionary<StatType, int> _stats = new();
        public WeaponStat RightHandStat { get; set; } = new();
        public WeaponStat LeftHandStat { get; set; } = new();

        public void SetStat(StatType type, int value)
        {
            _stats[type] = value;
        }

        public int GetStat(StatType type)
        {
            if(!_stats.ContainsKey(type))
                _stats[type] = 0;

            return _stats[type];
        }

        public IDictionary<StatType, int> GetAll()
        {
            return _stats.ToDictionary(x => x.Key, y => y.Value);
        }
    }
}
