using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>The attack roll a queued weapon ability has claimed, and the launcher values captured with it.</summary>
    public sealed class AbilityQueuedWeaponAttack
    {
        public int AttackIndex { get; init; }
        public uint Weapon { get; init; }
        public uint Projectile { get; init; } = OBJECT_INVALID;
        public int? TriggeringWeaponDamage { get; init; }
        public CombatDamageType? TriggeringWeaponDamageType { get; init; }
        public bool IsCurrentAttack { get; set; } = true;
    }
}
