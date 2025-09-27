using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.AI.ValueObjects;
using SWLOR.Shared.Domain.Character.Contracts;

namespace SWLOR.Shared.Domain.Character.ValueObjects
{
    public class Animator : IAnimator
    {
        public Animator() { }
        public Animator(VisualEffectType vfx, AnimationEvent animEvent, DurationType duration, float scale = 1.0f)
        {
            Vfx = vfx;
            Event = animEvent;
            Duration = duration;
            Scale = scale;
        }

        public VisualEffectType Vfx { get; set; }
        public AnimationEvent Event { get; set; }
        public DurationType Duration { get; set; }
        public float Scale { get; set; }
        public void SetLocalVariables(uint oObject)
        {
            SetLocalInt(oObject, Event.IdKey, (int)Vfx);
            SetLocalInt(oObject, Event.DurationKey, (int)Duration);
            SetLocalFloat(oObject, Event.ScaleKey, Scale);
        }
    }
}
