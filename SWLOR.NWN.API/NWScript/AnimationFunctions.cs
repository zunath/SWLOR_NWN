using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Plays the specified animation immediately.
        /// </summary>
        /// <param name="nAnimation">The animation to play (ANIMATION_* constant)</param>
        /// <param name="fSpeed">The speed of the animation (default: 1.0)</param>
        /// <param name="fSeconds">The duration of the animation in seconds (default: 0.0)</param>
        public static void PlayAnimation(Animation nAnimation, float fSpeed = 1.0f, float fSeconds = 0.0f)
        {
            global::NWN.Core.NWScript.PlayAnimation((int)nAnimation, fSpeed, fSeconds);
        }
    }
}
