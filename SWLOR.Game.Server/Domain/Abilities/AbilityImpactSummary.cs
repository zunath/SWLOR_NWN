using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Domain.Abilities
{
    public sealed class AbilityImpactSummary
    {
        public AbilityDetail Ability { get; set; }
        public SkillType SkillType { get; set; }
        public bool IsAreaAbility { get; set; }
        public bool IsSingleTargetAbility { get; set; }
        public int ImpactedTargetCount { get; set; }
        public int CriticalHitCount { get; set; }
        public int? TriggeringWeaponDamage { get; internal set; }
        public CombatDamageType? TriggeringWeaponDamageType { get; internal set; }

        /// <summary>
        /// Total damage this impact queued against its targets. Distinguishes "the impact
        /// visited a target" (ImpactedTargetCount, which records even zero-damage visits)
        /// from "the impact actually dealt damage" - the distinction needed to attribute a
        /// queued weapon ability's damage separately from the consuming weapon swing.
        /// </summary>
        public int AttributedDamage { get; set; }

        /// <summary>
        /// The creatures this impact visited.
        /// </summary>
        public IReadOnlyCollection<uint> ImpactedTargets { get; set; } = Array.Empty<uint>();

        public AbilityImpactSummary()
        {
            SkillType = SkillType.Invalid;
        }
    }
}
