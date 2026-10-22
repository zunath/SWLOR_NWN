using SWLOR.Game.Server.Service;

namespace SWLOR.CraftSim.Data
{
    /// <summary>The server's XP chart; GetDeltaXP needs the cap that CacheXPChartData sets at module load, so it runs once first.</summary>
    public static class ServerXp
    {
        private static readonly object Gate = new();
        private static bool _loaded;

        /// <summary>XP for an activity whose level is <paramref name="delta"/> above the crafter's rank.</summary>
        public static int ForDelta(int delta)
        {
            EnsureLoaded();
            return Skill.GetDeltaXP(delta);
        }

        private static void EnsureLoaded()
        {
            if (_loaded)
                return;

            lock (Gate)
            {
                if (_loaded)
                    return;

                Skill.CacheXPChartData();
                _loaded = true;
            }
        }
    }
}
