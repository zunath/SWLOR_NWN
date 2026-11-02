namespace SWLOR.Game.Server.Service.CraftService
{
    /// <summary>
    /// The Recipes window's level filter: option 1 is levels 0-9, options 2 to 5 are the next tens (10-19 to 40-49), and option 6 is 50+,
    /// matching the labels the window shows.
    /// </summary>
    public static class RecipeLevelBands
    {
        public const int HighestBand = 6;

        public static bool Contains(int band, int level)
        {
            if (band < 1 || band > HighestBand)
                return false;
            if (band == HighestBand)
                return level >= (HighestBand - 1) * 10;

            var low = (band - 1) * 10;
            return level >= low && level <= low + 9;
        }
    }
}
