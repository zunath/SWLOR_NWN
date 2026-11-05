using SWLOR.Game.Server.Service.AnimationService;

namespace SWLOR.Game.Server.Domain.Core
{
    /// <summary>
    /// Plays the visual effect an object carries for an animation event. The static facade
    /// <c>Service.AnimationPlayer</c> forwards to the container's instance of this interface.
    /// </summary>
    public interface IAnimationPlayerService
    {
        /// <summary>Applies the effect stored on the object for the event, if it has one.</summary>
        void Play(uint oObject, AnimationEvent animationEvent);
    }
}
