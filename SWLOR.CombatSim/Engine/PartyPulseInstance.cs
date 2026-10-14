using SWLOR.CombatSim.Effects;

namespace SWLOR.CombatSim.Engine
{
    public sealed class PartyPulseInstance
    {
        public AbilityEffect Effect { get; init; }
        public int? MaxTargets { get; init; }

        public int TicksLeft { get; set; }
        public double NextTickAt { get; set; }
    }
}
