using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Service.AnimationService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Plays the visual effect an object carries for an animation event. The logic lives in
    /// <see cref="IAnimationPlayerService"/>; the member here forwards to the container's instance.
    /// </summary>
    public static class AnimationPlayer
    {
        private static IAnimationPlayerService Instance => FacadeAccessor.Resolve<IAnimationPlayerService>();

        public static void Play(uint oObject, AnimationEvent animationEvent) => Instance.Play(oObject, animationEvent);
    }
}
