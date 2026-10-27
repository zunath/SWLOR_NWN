using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Crew rules the styles call. The crew update fills these in; until then every crew member counts for nothing
    /// and no crew-station seat is crewed.
    /// </summary>
    public static class SpaceCrew
    {
        /// <summary>
        /// The effectiveness of the crew working a ship's stations, as a multiplier (1.0 is normal). Command's
        /// Crew Morale raises it, and so will the captain's Leadership rank (<see cref="SpaceCommand.GetLeadershipBonusPercent"/>).
        /// </summary>
        public static double GetCrewEffectiveness(uint ship) =>
            EffectivenessMultiplier(
                Stat.GetStatAdjustment(ship, StatType.SpaceCrewEffectivenessPercentAdjustment),
                SpaceCommand.GetLeadershipBonusPercent(ship));

        /// <summary>The crew effectiveness multiplier for a Crew Morale percent and a Leadership percent.</summary>
        public static double EffectivenessMultiplier(int crewMoralePercent, int leadershipPercent) =>
            1.0 + (Math.Max(0, crewMoralePercent) + Math.Max(0, leadershipPercent)) / 100.0;

        /// <summary>Whether someone is working the crew-station seat that owns a hotbar slot (1-30).</summary>
        public static bool IsSeatCrewed(ShipStatus status, int slotNumber) => false;
    }
}
