using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>A creature's cast or channel between its start and its impact (the state <c>UsePerkFeat</c> runs on).</summary>
    public sealed class AbilityActiveActivation
    {
        public string ActivationId { get; init; }
        public AbilityDetail Ability { get; init; }
        public List<string> TelegraphIds { get; init; }
        public uint ResumeAttackTarget { get; init; }
        public bool IsAwaitingImpact { get; set; }
        public WeaponAbilityTraitActivation WeaponTraits { get; init; }
    }
}
