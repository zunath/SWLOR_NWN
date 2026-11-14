using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Native
{
    /// <summary>The damage type and DMG rating a native weapon roll deals, and whether the DMG is an item property's.</summary>
    internal readonly struct WeaponDamageProfile
    {
        public CombatDamageType DamageType { get; }
        public int Damage { get; }
        public bool HasItemDamage { get; }

        public WeaponDamageProfile(CombatDamageType damageType, int damage, bool hasItemDamage = true)
        {
            DamageType = damageType;
            Damage = damage;
            HasItemDamage = hasItemDamage;
        }
    }
}
