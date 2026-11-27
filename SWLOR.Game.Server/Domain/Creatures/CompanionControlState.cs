using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>
    /// What the companion control keeps for one registered companion: its mode, the targets it was ordered or allowed to
    /// engage, the progress of its pursuit and its cached hostile abilities. Held by <see cref="ICompanionControlStoreService"/>.
    /// </summary>
    public sealed class CompanionControlState
    {
        public CompanionMode Mode { get; set; } = CompanionMode.Follow;
        public bool AbilitiesEnabled { get; set; } = true;
        public uint AttackNearestTarget { get; set; } = OBJECT_INVALID;
        public uint OwnerAssistTarget { get; set; } = OBJECT_INVALID;
        public Dictionary<uint, DateTime> DefensiveThreats { get; } = new();
        public uint TrackedTarget { get; set; } = OBJECT_INVALID;
        public float LastDistanceToTarget { get; set; } = float.MaxValue;
        public DateTime LastProgressAt { get; set; }
        public DateTime LastOffensiveActivityAt { get; set; }
        public uint LastMasterArea { get; set; } = OBJECT_INVALID;
        public DateTime ExplicitOrderUntil { get; set; }
        public bool DefensiveReactionPending { get; set; }
        public int HostileAbilityFeatCount { get; set; } = -1;
        public int HostileAbilityFeatChecksum { get; set; }
        public List<(FeatType Feat, AbilityDetail Ability)> HostileAbilities { get; } = new();
    }
}
