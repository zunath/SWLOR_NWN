using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Foundation
{
    /// <summary>
    /// Plays the visual effect an object carries for an animation event. Static callers reach the container's
    /// instance through <c>GameServices.AnimationPlayer</c>.
    /// </summary>
    public interface IAnimationPlayerService
    {
        /// <summary>Applies the effect stored on the object for the event, if it has one.</summary>
        void Play(uint oObject, AnimationEvent animationEvent);
    }
}
