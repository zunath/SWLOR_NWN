
// ReSharper disable once CheckNamespace
namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Destroy oObject (irrevocably).
        /// This will not work on modules and areas.
        /// </summary>
        /// <remarks>
        /// This is a custom implementation that extends the base NWScript DestroyObject function
        /// to call a cleanup script after object destruction for proper event handling.
        /// </remarks>
        /// <param name="oDestroy">The object to destroy.</param>
        /// <param name="fDelay">The delay before destruction (default: 0.0f).</param>
        public static void DestroyObject(uint oDestroy, float fDelay = 0.0f)
        {
            global::NWN.Core.NWScript.DestroyObject(oDestroy, fDelay);
            ExecuteScript("object_destroyed", oDestroy);
        }
    }
}