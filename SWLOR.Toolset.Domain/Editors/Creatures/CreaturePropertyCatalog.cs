namespace SWLOR.Toolset.Domain.Editors.Creatures
{
    /// <summary>Item-property ids intentionally surfaced by creature Stats.</summary>
    public static class CreaturePropertyCatalog
    {
        public const int FocusPoints = 91;
        public const int Stamina = 92;
        public const int Damage = 93;
        public const int Defense = 94;
        public const int HitPoints = 96;
        public const int Delay = 98;
        public const int Level = 99;
        public const int DamageStat = 103;
        public const int Attack = 111;
        public const int ForceAttack = 112;
        public const int Evasion = 117;
        public const int CombatReadiness = 118;
        public const int NpcSkill = 125;
        public const int Resistance = 133;
        public const int WeaponDamageType = 134;

        public const int StatSkinSlot = 131072;
        public const int StatSkinBaseItem = 73;
        public const int MainWeaponSlot = 16384;
        public const int OffWeaponSlot = 32768;
        public const int CreatureWeaponSlot = 65536;

        public static IReadOnlySet<int> SurfacedSkinProperties { get; } = new HashSet<int>
        {
            FocusPoints, Stamina, Defense, HitPoints, Level, Attack, ForceAttack,
            Evasion, CombatReadiness, NpcSkill, Resistance
        };

        public static IReadOnlySet<int> SurfacedWeaponProperties { get; } = new HashSet<int>
        {
            Damage, Delay, DamageStat, WeaponDamageType
        };

        /// <summary>Ordinary engine item effects found on stat skins and intentionally left intact.</summary>
        public static IReadOnlySet<int> PreservedSkinProperties { get; } = new HashSet<int>
        {
            20, 22, 35, 37, 51, 75
        };

        /// <summary>Ordinary engine weapon effects found on natural weapons and intentionally left intact.</summary>
        public static IReadOnlySet<int> PreservedWeaponProperties { get; } = new HashSet<int>
        {
            16, 20, 21, 24, 37, 48, 56, 67, 72, 82, 83
        };

        public static int DecodeResistance(int stored) => stored > 100 ? -(stored - 100) : stored;

        public static int EncodeResistance(int value) => value < 0 ? 100 + Math.Abs(value) : value;
    }
}
