using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// What the ground skills with a space job add per rank (plan section 4, W11n). Each bonus grows evenly with the
    /// rank of whoever works the module (the crew member at its seat, otherwise the pilot; a droid and an NPC ship have
    /// no rank) and stops at rank 50. The sizes follow existing standards and are checked by
    /// <c>tools/SpaceRevampBalance/checks.py</c> section Q:
    /// <list type="bullet">
    /// <item>Module output (Shield, Hull and Repair Field repairs, the Hypermatter Injector, Jammer and E-War Array jams,
    /// Target Painter paints): +0.2% per
    /// rank, up to +10%, below the Command style's Repair Efficiency line (+15% for three perk ranks), so a ground skill
    /// never outweighs an SP purchase.</item>
    /// <item>Power routing: +0.5% per Engineering rank, up to +25%, the size of Leadership's bonus to the Command
    /// style's orders. Only the routing's gain grows; its penalties stay, so no mode kills faster than Balanced.</item>
    /// <item>Cloak reveal: +1% per Espionage rank, up to +50%, the Probe Launcher's scan bonus
    /// (<see cref="CloakTuning.EspionageRevealPercentPerRank"/>).</item>
    /// </list>
    /// </summary>
    public static class ShipSeatSkillRules
    {
        /// <summary>The rank at which every seat-skill bonus stops.</summary>
        public const int MaximumRank = 50;

        /// <summary>Engineering 50: repairs and the Hypermatter Injector restore this much more (percent).</summary>
        public const int EngineeringSupportMaximumPercent = 10;

        /// <summary>Engineering 50: the power routing's gain is this much larger (percent of the gain).</summary>
        public const int EngineeringRoutingMaximumPercent = 25;

        /// <summary>Espionage 50: a Jammer's or E-War Array's Accuracy penalty and a Target Painter's damage taken are this much stronger (percent).</summary>
        public const int EspionageElectronicWarfareMaximumPercent = 10;

        /// <summary>The bonus to repairs and capacitor restored for an Engineering rank: +0.2% per rank, up to +10%.</summary>
        public static int EngineeringSupportPercent(int engineeringRank) =>
            ScaleByRank(engineeringRank, EngineeringSupportMaximumPercent);

        /// <summary>The bonus to the power routing's gain for an Engineering rank: +0.5% per rank, up to +25%.</summary>
        public static int EngineeringRoutingPercent(int engineeringRank) =>
            ScaleByRank(engineeringRank, EngineeringRoutingMaximumPercent);

        /// <summary>The bonus to jam and paint strength for an Espionage rank: +0.2% per rank, up to +10%.</summary>
        public static int EspionageElectronicWarfarePercent(int espionageRank) =>
            ScaleByRank(espionageRank, EspionageElectronicWarfareMaximumPercent);

        /// <summary>The bonus to the range at which a ship sees through a cloak for an Espionage rank: +1% per rank, up to +50%.</summary>
        public static int EspionageRevealPercent(int espionageRank) =>
            Math.Clamp(espionageRank, 0, CloakTuning.EspionageRevealMaximumRank) * CloakTuning.EspionageRevealPercentPerRank;

        /// <summary>
        /// The Espionage rank a ship sees through cloaks with: the better of its pilot's and its Sensors officer's, so
        /// seating an officer never shortens the ship's sight.
        /// </summary>
        public static int ShipEspionageRank(int pilotRank, int sensorsOfficerRank) => Math.Max(pilotRank, sensorsOfficerRank);

        /// <summary>The skill whose rank adds to a module's output, or <see cref="SkillType.Invalid"/> when none does.</summary>
        public static SkillType GetBonusSkill(ShipModuleType type) => type switch
        {
            ShipModuleType.ShieldRepairer or ShipModuleType.HullRepairer or ShipModuleType.RepairFieldGenerator
                or ShipModuleType.HypermatterInjector => SkillType.Engineering,
            ShipModuleType.Jammer or ShipModuleType.TargetPainter or ShipModuleType.CapitalEwar => SkillType.Espionage,
            _ => SkillType.Invalid,
        };

        /// <summary>The percent a rank in the module's bonus skill adds to its output, 0 for a module with none.</summary>
        public static int GetModuleBonusPercent(ShipModuleType type, int rank) => GetBonusSkill(type) switch
        {
            SkillType.Engineering => EngineeringSupportPercent(rank),
            SkillType.Espionage => EspionageElectronicWarfarePercent(rank),
            _ => 0,
        };

        private static int ScaleByRank(int rank, int maximumPercent) =>
            (int)Math.Round(Math.Clamp(rank, 0, MaximumRank) * maximumPercent / (double)MaximumRank, MidpointRounding.AwayFromZero);
    }
}
