using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Service.AnimationService
{
    public class Animator : IAnimator
    {
        public Animator() { }
        public Animator(VisualEffect vfx, AnimationEvent animEvent, DurationType duration, float scale = 1.0f)
        {
            Vfx = vfx;
            Event = animEvent;
            Duration = duration;
            Scale = scale;
        }

        public VisualEffect Vfx { get; set; }
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
