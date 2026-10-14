using SWLOR.CombatSim.Effects;

namespace SWLOR.CombatSim.Engine
{
    public sealed class PartyPulseInstance
    {
        public AbilityEffect Effect { get; init; }
        public int? MaxTargets { get; init; }

        /// <summary>Refreshes the zone's damage reduction instead of healing; the game runs these on their own cadence.</summary>
        public bool RefreshesDamageReduction { get; init; }
        public int TicksLeft { get; set; }
        public double NextTickAt { get; set; }
    }
}
