using SWLOR.Game.Server.Service.DroidService;

namespace SWLOR.Game.Server.Service.StationService
{
    /// <summary>
    /// What an industrial droid assigned to a station building adds (plan sections 25 and 27): the hook the station
    /// buildings fill (W13b). Each takes the droid's Industry (and Efficiency Protocol's percent where output counts) and
    /// returns a percent; the numbers are <see cref="DroidIndustryTuning"/>'s, from tools/SpaceRevampBalance/droids.py.
    /// An assigned droid is posted like a ship's droid crew, so it never takes its owner's follow slot.
    /// </summary>
    public static class StationDroidRules
    {
        /// <summary>The deck slots whose building takes a droid.</summary>
        public static bool TakesDroid(StationDeckSlotType slot) =>
            slot is StationDeckSlotType.Shipyard or StationDeckSlotType.AsteroidDrill;

        /// <summary>The Shipyard: how much faster an assigned droid builds (+30% at 100 Industry).</summary>
        public static int ShipyardBuildSpeedPercent(int industry) =>
            Round(DroidIndustryTuning.ShipyardBuildSpeedPercentPerIndustry * Math.Max(0, industry));

        /// <summary>The Asteroid Drill: the output an assigned droid adds (+50% at 100 Industry, raised by Efficiency Protocol).</summary>
        public static int DrillOutputPercent(int industry, int efficiencyPercent) =>
            Round(DroidIndustryTuning.DrillOutputPercentPerIndustry * DroidIndustryRules.OutputIndustry(industry, efficiencyPercent));

        private static int Round(double value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }
}
