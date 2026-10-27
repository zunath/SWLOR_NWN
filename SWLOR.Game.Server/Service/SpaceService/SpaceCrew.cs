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
        /// Crew Morale and Leadership rank scaling will raise it.
        /// </summary>
        public static double GetCrewEffectiveness(uint ship) => 1.0;

        /// <summary>Whether someone is working the crew-station seat that owns a hotbar slot (1-30).</summary>
        public static bool IsSeatCrewed(ShipStatus status, int slotNumber) => false;
    }
}
