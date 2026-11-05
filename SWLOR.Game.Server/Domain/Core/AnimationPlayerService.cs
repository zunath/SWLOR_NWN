using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Domain.Core
{
    /// <summary>
    /// Animation player: the logic that used to be static in <c>Service.AnimationPlayer</c>, moved here unchanged.
    /// The facade forwards every call to the container's instance.
    /// </summary>
    public class AnimationPlayerService : IAnimationPlayerService
    {
        public void Play(uint oObject, AnimationEvent animationEvent)
        {
            var vfx = (VisualEffect)GetLocalInt(oObject, animationEvent.IdKey);

            // technically 0 could be valid, but we can't differentiate between err and blur here; no blurring on death allowed
            if (vfx != 0)
            {
                var duration = (DurationType)GetLocalInt(oObject, animationEvent.DurationKey);
                var scale = GetLocalFloat(oObject, animationEvent.ScaleKey);
                var effect = EffectVisualEffect(vfx, false, scale);
                ApplyEffectToObject(duration, effect, oObject);
            }
        }
    }
}
