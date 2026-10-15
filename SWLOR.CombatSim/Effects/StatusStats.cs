using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.CombatSim.Effects
{
    /// <summary>
    /// Reads a status effect's stats from its definition in SWLOR.Game.Server, so a modeled status can't drift from the
    /// game. Only stats the simulator reads have any effect.
    /// </summary>
    public static class StatusStats
    {
        private static readonly Dictionary<Type, IReadOnlyDictionary<StatType, int>> Cache = new();

        public static IReadOnlyDictionary<StatType, int> Get(Type statusType)
        {
            if (Cache.TryGetValue(statusType, out var stats))
                return stats;

            if (!typeof(StatusEffectBase).IsAssignableFrom(statusType))
                throw new InvalidDataException($"'{statusType.Name}' is not a status effect.");

            var status = (StatusEffectBase)Activator.CreateInstance(statusType)!;
            stats = new Dictionary<StatType, int>(status.StatGroup.Stats);
            Cache[statusType] = stats;
            return stats;
        }
    }
}
