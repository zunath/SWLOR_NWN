using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>
    /// Lets a window push a value to the client only when it differs from what the client already shows. Every
    /// assignment to a bound property sends a bind, so the beat loop checks first.
    /// </summary>
    public static class PerformanceBindCache
    {
        /// <summary>Stores <paramref name="value"/> in <paramref name="shown"/> and returns true when it was different.</summary>
        public static bool Changed<T>(ref T shown, T value)
        {
            if (EqualityComparer<T>.Default.Equals(shown, value))
                return false;

            shown = value;
            return true;
        }
    }
}
