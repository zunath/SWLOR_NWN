using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Enums;

namespace SWLOR.Shared.Domain.Character.ValueObjects
{
    public class StatGroup
    {
        private readonly Dictionary<StatType, int> _stats = new();

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

        public int GetStat(AbilityType type)
        {
            switch (type)
            {
                case AbilityType.Might:
                    return GetStat(StatType.Might);
                    break;
                case AbilityType.Perception:
                    return GetStat(StatType.Perception);
                    break;
                case AbilityType.Vitality:
                    return GetStat(StatType.Vitality);
                    break;
                case AbilityType.Agility:
                    return GetStat(StatType.Agility);
                    break;
                case AbilityType.Willpower:
                    return GetStat(StatType.Willpower);
                    break;
                case AbilityType.Social:
                    return GetStat(StatType.Social);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public IDictionary<StatType, int> GetAll()
        {
            return _stats.ToDictionary(x => x.Key, y => y.Value);
        }
    }
}
