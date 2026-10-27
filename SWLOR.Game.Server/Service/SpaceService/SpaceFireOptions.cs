using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// How a space ability fires its weapon (<see cref="SpaceAbilities.FireWeapon"/>).
    /// </summary>
    public class SpaceFireOptions
    {
        /// <summary>The shot belongs to a hostile ability, not to the weapon's own cycle.</summary>
        public bool IsAbility { get; set; } = true;

        /// <summary>The ability is an area ability, for the single-target and area damage stats.</summary>
        public bool IsArea { get; set; }

        /// <summary>
        /// Whether the shot consumes pending riders. An ability shot leaves them for the next weapon cycle,
        /// the way a queued ground ability waits for the next auto-attack.
        /// </summary>
        public bool ConsumesRiders { get; set; }

        /// <summary>Overrides the weapon's damage type. Null keeps the weapon's own.</summary>
        public CombatDamageType? DamageType { get; set; }
    }
}
