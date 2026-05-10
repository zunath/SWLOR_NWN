using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.AIService
{
    public sealed class AIContext
    {
        public uint Self { get; init; }
        public uint CurrentTarget { get; init; }
        public uint LowestHPAlly { get; init; }
        public uint AllyWithTreatmentKit1Status { get; init; }
        public uint AllyWithTreatmentKit2Status { get; init; }
        public float SelfHPPercentage { get; init; }
        public float LowestHPAllyPercentage { get; init; }
        public int AllyCount { get; init; }
        public FeatType ActiveConcentrationFeat { get; init; } = FeatType.Invalid;
        public AIPhaseType Phase { get; set; } = AIPhaseType.Damage;
    }
}
