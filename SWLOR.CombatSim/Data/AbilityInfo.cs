using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.CombatSim.Data
{
    /// <summary>
    /// Cost and timing data for one ability rank, read from the game server's ability definitions.
    /// </summary>
    public sealed class AbilityInfo
    {
        public FeatType Feat { get; init; }
        public string Name { get; init; }
        public string PerkName { get; init; }
        public string RecastGroup { get; init; }
        public int SkillRequirement { get; init; }
        public int FP { get; init; }
        public int STM { get; init; }
        public float RecastSeconds { get; init; }
        public float CastSeconds { get; init; }
        public bool IsHostile { get; init; }
        public bool IsHealing { get; init; }
        public string ForceAffinity { get; init; }
    }
}
