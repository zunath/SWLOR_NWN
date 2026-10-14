using SWLOR.Game.Server.Service.SkillService;
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

        /// <summary>A weapon ability: paid when queued, it lands on the caster's next landed attack.</summary>
        public bool IsQueuedWeapon { get; init; }

        /// <summary>What the game computes at runtime and the simulator could not read (for example "recast"); null when timings were read.</summary>
        public string UnreadableTiming { get; init; }

        public SkillType SkillType { get; init; }
        public bool IsHostile { get; init; }
        public bool IsHealing { get; init; }
        public string ForceAffinity { get; init; }
    }
}
