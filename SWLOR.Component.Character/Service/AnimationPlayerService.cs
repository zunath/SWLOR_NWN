using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.ValueObjects;

namespace SWLOR.Component.Character.Service
{
    public class AnimationPlayerService : IAnimationPlayerService
    {
        public void Play(uint oObject, AnimationEvent animationEvent)
        {
            var vfx = (VisualEffectType)GetLocalInt(oObject, animationEvent.IdKey);

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
