using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Play nAnimation immediately.
        ///   - nAnimation: ANIMATION_*
        ///   - fSpeed
        ///   - fSeconds
        /// </summary>
        public static void PlayAnimation(Animation nAnimation, float fSpeed = 1.0f, float fSeconds = 0.0f)
        {
            global::NWN.Core.NWScript.PlayAnimation((int)nAnimation, fSpeed, fSeconds);
        }
    }
}
