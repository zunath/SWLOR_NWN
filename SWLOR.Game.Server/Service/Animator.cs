using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Core.NWScript.Enum;
using static SWLOR.Game.Server.Core.NWScript.NWScript;


namespace SWLOR.Game.Server.Service.AnimationService
{
    public class Animator : IAnimator
    {
        public Animator() { }
        public Animator(VisualEffect vfx, AnimationEvent animEvent, DurationType duration)
        {
            Vfx = vfx;
            Event = animEvent;
            Duration = duration;
        }

        public VisualEffect Vfx { get; set; }
        public AnimationEvent Event { get; set; }

        public DurationType Duration { get; set; }

        public void SetLocalVariables(uint oObject)
        {
            SetLocalInt(oObject, Event.IdKey, (int)Vfx);
            SetLocalInt(oObject, Event.DurationKey, (int)Duration);
        }
    }
}
