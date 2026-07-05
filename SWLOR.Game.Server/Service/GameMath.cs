using System;

namespace SWLOR.Game.Server.Service
{
    public static class GameMath
    {
        public static int PercentOf(int value, int percent)
        {
            return Math.Max(1, (int)Math.Ceiling(value * (percent / 100f)));
        }
    }
}
