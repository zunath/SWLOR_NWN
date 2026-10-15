using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.CombatSim.Effects
{
    /// <summary>
    /// Reads a status effect's stats from its definition in SWLOR.Game.Server, so a modeled status can't drift from the
    /// game. Only stats the simulator reads have any effect, as with perks: hit chance and damage come from each use's
    /// damage profile, so attack and damage stats (Conduit Stance's -10% Attack, Infinite Conduit's +20 DMG) are not
    /// applied, and a scenario's damage profiles describe the build as it fights, stance included.
    /// </summary>
    public static class StatusStats
    {
        private static readonly Dictionary<Type, IReadOnlyDictionary<StatType, int>> Cache = new();

        public static IReadOnlyDictionary<StatType, int> Get(Type statusType)
        {
            if (Cache.TryGetValue(statusType, out var stats))
                return stats;

            var status = Create(statusType);
            stats = new Dictionary<StatType, int>(status.StatGroup.Stats);
            Cache[statusType] = stats;
            return stats;
        }

        /// <summary>Whether the status is a stance; a creature holds one stance at a time.</summary>
        public static bool IsStance(Type statusType) => Create(statusType).SourceType == StatusEffectSourceType.Stance;

        private static StatusEffectBase Create(Type statusType)
        {
            if (!typeof(StatusEffectBase).IsAssignableFrom(statusType))
                throw new InvalidDataException($"'{statusType.Name}' is not a status effect.");

            return (StatusEffectBase)Activator.CreateInstance(statusType)!;
        }
    }
}
