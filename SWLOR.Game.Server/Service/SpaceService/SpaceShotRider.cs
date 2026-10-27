namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// A pending "on your next weapon hit" effect stored on the ship status. The next landed weapon shot that
    /// matches consumes it: <see cref="BonusDmg"/> joins that shot's DMG, <see cref="ExtraDamage"/> lands as a
    /// second instance of damage on the same target, <see cref="ExtraEnmity"/> is added threat, and the handler
    /// registered under <see cref="Key"/> (see <see cref="SpaceAbilities.RegisterRiderHandler"/>) runs for anything else.
    /// </summary>
    public class SpaceShotRider
    {
        /// <summary>Names the rider. Arming a rider with a key the ship already holds replaces it.</summary>
        public string Key { get; set; }

        /// <summary>Added to the consuming shot's DMG before it rolls.</summary>
        public int BonusDmg { get; set; }

        /// <summary>A second instance of damage on the consuming shot's target. It does not roll again.</summary>
        public int ExtraDamage { get; set; }

        /// <summary>Flat threat added to the consuming shot's target.</summary>
        public int ExtraEnmity { get; set; }

        /// <summary>A value for the key's handler (a duration, a percent).</summary>
        public int Amount { get; set; }

        /// <summary>A second value for the key's handler.</summary>
        public int SecondaryAmount { get; set; }

        /// <summary>Only shots of this weapon family consume the rider.</summary>
        public SpaceWeaponFilter Filter { get; set; } = SpaceWeaponFilter.Any;

        /// <summary>The rider lapses unconsumed at this time (UTC).</summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>Whether a shot of this family can consume the rider at this time.</summary>
        public bool Matches(SpaceWeaponFamily family, DateTime now)
        {
            if (now >= ExpiresAt)
                return false;

            return Filter switch
            {
                SpaceWeaponFilter.Energy => family == SpaceWeaponFamily.Energy,
                SpaceWeaponFilter.Launcher => family == SpaceWeaponFamily.Launcher,
                _ => true,
            };
        }
    }
}
