using SWLOR.NWN.API.Engine;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Gets the orientation value from the location.
        /// </summary>
        /// <param name="lLocation">The location to get the orientation from</param>
        /// <returns>The orientation value</returns>
        public static float GetFacingFromLocation(Location lLocation)
        {
            return global::NWN.Core.NWScript.GetFacingFromLocation(lLocation);
        }
    }
}