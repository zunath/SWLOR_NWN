using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.CombatSim.Engine
{
    /// <summary>A status with stats on a combatant, read from the game's status effect definition.</summary>
    public sealed class ActiveStatus
    {
        public IReadOnlyDictionary<StatType, int> Stats { get; init; }

        /// <summary>When the status ends; infinity for one held all fight, such as a stance.</summary>
        public double ExpiresAt { get; init; }
    }
}
