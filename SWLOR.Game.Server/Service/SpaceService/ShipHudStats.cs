using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The numbers the Ship HUD's Stats tab shows for a player's ship. <see cref="FromStatus"/> copies what the ship
    /// status holds; the caller fills the values that also depend on the pilot (attributes and stats).
    /// </summary>
    public class ShipHudStats
    {
        public int Hull { get; set; }
        public int MaxHull { get; set; }
        public int Shield { get; set; }
        public int MaxShield { get; set; }
        public int Capacitor { get; set; }
        public int MaxCapacitor { get; set; }
        public double ShieldRegenPerSecond { get; set; }
        public double CapacitorRegenPerSecond { get; set; }

        public int EnergyAttack { get; set; }
        public int LauncherAttack { get; set; }
        public int EnergyAccuracy { get; set; }
        public int LauncherAccuracy { get; set; }
        public int Defense { get; set; }
        public int Evasion { get; set; }
        public int CriticalRate { get; set; }

        public int ThermalResistance { get; set; }
        public int EmResistance { get; set; }
        public int ExplosiveResistance { get; set; }
        public int ThermalDamagePercent { get; set; }
        public int EmDamagePercent { get; set; }
        public int ExplosiveDamagePercent { get; set; }

        /// <summary>The movement multiplier (1.0 is normal speed).</summary>
        public double SpeedMultiplier { get; set; } = 1.0;

        /// <summary>The cargo hold size, or null when the hull has none listed.</summary>
        public int? Cargo { get; set; }

        public int HighFitted { get; set; }
        public int HighSlots { get; set; }
        public int MidFitted { get; set; }
        public int MidSlots { get; set; }
        public int LowFitted { get; set; }
        public int LowSlots { get; set; }

        /// <summary>
        /// Copies the pools, rating, resistances, damage bonuses, speed and fitted module counts a ship status holds.
        /// The shield maximum is the effective one, so an active Bulwark Shield Generator shows.
        /// </summary>
        public static ShipHudStats FromStatus(ShipStatus status, DateTime now)
        {
            var stats = new ShipHudStats
            {
                Hull = status.Hull,
                MaxHull = status.MaxHull,
                Shield = status.Shield,
                MaxShield = status.GetEffectiveMaxShield(now),
                Capacitor = status.Capacitor,
                MaxCapacitor = status.MaxCapacitor,
                Defense = status.Defense,
                Evasion = status.Evasion,
                SpeedMultiplier = status.SpeedMultiplier,
                HighFitted = status.HighPowerModules?.Count ?? 0,
                MidFitted = status.MidPowerModules?.Count ?? 0,
                LowFitted = status.LowPowerModules?.Count ?? 0,
            };

            stats.ThermalResistance = Resistance(status, ResistanceType.Thermal, now);
            stats.EmResistance = Resistance(status, ResistanceType.EM, now);
            stats.ExplosiveResistance = Resistance(status, ResistanceType.Explosive, now);
            stats.ThermalDamagePercent = DamagePercent(status, CombatDamageType.Thermal);
            stats.EmDamagePercent = DamagePercent(status, CombatDamageType.EM);
            stats.ExplosiveDamagePercent = DamagePercent(status, CombatDamageType.Explosive);
            return stats;
        }

        private static int Resistance(ShipStatus status, ResistanceType type, DateTime now)
        {
            var resistance = 0;
            status.Resistances?.TryGetValue(type, out resistance);
            return SpaceCombatMath.HardenedResistance(resistance, status.HardenedAmount, status.HardenedUntil, now);
        }

        private static int DamagePercent(ShipStatus status, CombatDamageType type)
        {
            var percent = 0;
            status.DamageBonusPercent?.TryGetValue(type, out percent);
            return percent;
        }
    }
}
