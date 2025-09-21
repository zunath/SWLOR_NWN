using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Shared.Core.Contracts
{
    public interface IAnimationPlayerService
    {
        /// <summary>
        /// Plays an animation on an object.
        /// </summary>
        /// <param name="oObject">The object to play the animation on.</param>
        /// <param name="animationEvent">The animation event to play.</param>
        void Play(uint oObject, AnimationEvent animationEvent);
    }
}
