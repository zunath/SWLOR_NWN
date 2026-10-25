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
        public SWLOR.Game.Server.Service.AbilityService.MeleeTechniqueDetail Technique { get; init; }
        public int? SourceWeaponPower { get; init; }
        public int ImpactStatusDuration { get; init; }
        public Func<SWLOR.Game.Server.Service.StatusEffectService.IStatusEffect> ImpactStatusFactory { get; init; }
        public IReadOnlyList<Type> ImpactStatusTypes { get; init; } = Array.Empty<Type>();
        /// <summary>The source's configured instance of an impact status (Rending Strike's Exposed), when it supplies one.</summary>
        public Func<SWLOR.Game.Server.Service.StatusEffectService.IStatusEffect> WeaponStatusFactory { get; init; }
        /// <summary>The skill the source passes to its combat impact; Rending Strike's definition itself declares none.</summary>
        public SkillType WeaponImpactSkillType { get; init; }
        /// <summary>Savage Cleave II-III restore STM per secondary target hit; the simulator refuses multi-target hits rather than guess the amount.</summary>
        public bool RestoresSecondaryTargetStamina { get; init; }
        public bool InterruptsTarget { get; init; }
        /// <summary>Source DMG added to the impact's power against a target already affected by a control effect.</summary>
        public int ExtraDamageIfTargetControlled { get; init; }
        /// <summary>A second status the impact applies to a target already affected by a required status or category.</summary>
        public ConditionalTargetStatus ConditionalTargetStatus { get; init; }
        /// <summary>Source riders the weapon simulation does not model; a weapon-calibrated use of the ability is refused.</summary>
        public IReadOnlyList<string> UnmodeledRiders { get; init; } = Array.Empty<string>();
        public bool IsAreaAbility { get; init; }
        public float WeaponAreaRadius { get; init; }
        public int MaximumAreaTargets { get; init; }
        public string Name { get; init; }
        public string PerkName { get; init; }
        public string RecastGroup { get; init; }
        public int SkillRequirement { get; init; }
        public int FP { get; init; }
        public int STM { get; init; }

        /// <summary>The ability lists its FP requirement before its STM one, so the game pays (and Conduit restores) FP first.</summary>
        public bool PaysFpFirst { get; init; }
        public float RecastSeconds { get; init; }
        public float CastSeconds { get; init; }
        /// <summary>Effects and costs begin at channel start; the caster remains occupied until CastSeconds ends.</summary>
        public bool IsChanneled { get; init; }

        /// <summary>Seconds between paying for the ability and its impact (Force Leap's jump); the caster is busy throughout.</summary>
        public float ImpactDelaySeconds { get; init; }

        /// <summary>A weapon ability: paid when queued, it lands on the caster's next landed attack.</summary>
        public bool IsQueuedWeapon { get; init; }

        /// <summary>What the game computes at runtime and the simulator could not read (for example "recast"); null when timings were read.</summary>
        public string UnreadableTiming { get; init; }

        public SkillType SkillType { get; init; }
        public bool IsHostile { get; init; }

        /// <summary>The game aims it at a creature or a location; false for self-centered areas and queued weapon abilities.</summary>
        public bool IsAimed { get; init; }
        public bool IsHealing { get; init; }
        public string ForceAffinity { get; init; }
        public SWLOR.Game.Server.Service.CombatService.TwinPistolAbilitySpec TwinPistol { get; init; }
    }
}
