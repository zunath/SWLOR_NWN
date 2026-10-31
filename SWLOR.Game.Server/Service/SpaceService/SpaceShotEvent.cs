namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// A resolved weapon shot, as <see cref="SpaceCombatEvents.ShotEvaded"/> and
    /// <see cref="SpaceCombatEvents.ShotLanded"/> report it.
    /// </summary>
    public class SpaceShotEvent
    {
        public uint Attacker { get; set; }
        public uint Target { get; set; }

        /// <summary>The weapon family, damage type and flags of the shot.</summary>
        public SpaceWeaponShot Shot { get; set; }

        /// <summary>The roll. Shield and hull damage are filled in on a landed event.</summary>
        public ShipShotResult Result { get; set; }

        public SpaceWeaponFamily Family => Shot?.Family ?? SpaceWeaponFamily.Energy;

        /// <summary>
        /// The creature whose perks the shot's procs read: the crew member who fired it, or the attacker. A droid's shot
        /// keeps its OBJECT_INVALID source (SpaceCrew.GetStatSource), so no perk proc fires from it; only an event with no
        /// roll falls back to the attacker.
        /// </summary>
        public uint StatSource => Result?.StatSource ?? Attacker;

        /// <summary>True when a hostile ability fired the shot rather than the weapon's own cycle.</summary>
        public bool FromAbility => Shot?.IsAbility ?? false;
    }
}
