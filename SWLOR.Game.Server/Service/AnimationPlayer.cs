using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.AnimationService;

namespace SWLOR.Game.Server.Service
{
    public static class AnimationPlayer
    {
        public static void Play(uint oObject, AnimationEvent animationEvent)
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
