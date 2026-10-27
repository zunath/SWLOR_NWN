using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// What a weapon module fires: the inputs <see cref="Space.ResolveWeaponShot"/> needs besides the two ships.
    /// </summary>
    public class SpaceWeaponShot
    {
        public SpaceWeaponFamily Family { get; set; } = SpaceWeaponFamily.Energy;

        /// <summary>Thermal, EM or Explosive.</summary>
        public CombatDamageType DamageType { get; set; } = CombatDamageType.Thermal;

        /// <summary>The module's DMG, with any module bonus already added.</summary>
        public int Dmg { get; set; }

        /// <summary>The module's rank. Drives the critical rate and the damage formula's delta cap.</summary>
        public int Rank { get; set; }

        /// <summary>The module's own Attack, added to the ship's.</summary>
        public int Attack { get; set; }

        /// <summary>The module's own Accuracy, added to the ship's.</summary>
        public int Accuracy { get; set; }

        /// <summary>The size the weapon fires as. Null means the firing ship's size.</summary>
        public ShipSize? Size { get; set; }

        /// <summary>All damage goes to the hull.</summary>
        public bool ShieldBypass { get; set; }

        /// <summary>No size hit or damage penalty against larger targets.</summary>
        public bool IgnoreSizePenalty { get; set; }

        /// <summary>The shield loses this many points per point of damage it absorbs.</summary>
        public double ShieldDamageMultiplier { get; set; } = 1.0;

        /// <summary>A hostile ability fired this shot, not the weapon's own cycle.</summary>
        public bool IsAbility { get; set; }

        /// <summary>The ability is an area ability (it picks the area or single-target damage stat).</summary>
        public bool IsArea { get; set; }

        /// <summary>Whether the shot, once it lands, consumes pending riders (see <see cref="SpaceShotRider"/>).</summary>
        public bool ConsumesRiders { get; set; } = true;

        /// <summary>A copy of the shot with more DMG.</summary>
        public SpaceWeaponShot WithBonusDmg(int bonus)
        {
            var copy = (SpaceWeaponShot)MemberwiseClone();
            copy.Dmg += bonus;
            return copy;
        }
    }
}
