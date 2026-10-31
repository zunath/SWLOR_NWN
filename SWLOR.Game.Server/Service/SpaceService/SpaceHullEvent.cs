namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// A ship's hull took damage. <see cref="CrossedBelow"/> answers "reduced below 25% hull" style triggers.
    /// </summary>
    public class SpaceHullEvent
    {
        public uint Ship { get; set; }
        public uint Attacker { get; set; }
        public int HullBefore { get; set; }
        public int HullAfter { get; set; }
        public int MaxHull { get; set; }

        /// <summary>True when this damage took the hull from at or above the percent to below it.</summary>
        public bool CrossedBelow(int thresholdPercent) =>
            SpaceCombatMath.CrossedHullThreshold(HullBefore, HullAfter, MaxHull, thresholdPercent);
    }
}
