using SWLOR.Core.NWScript.Enum;
using SWLOR.Core.NWScript.Enum.VisualEffect;

namespace SWLOR.Core.Service.AnimationService
{
    public interface IAnimator
    {
        VisualEffect Vfx { get; set; }

        AnimationEvent Event { get; set; }

        DurationType Duration { get; set; }

        public float Scale { get; set; }

        public void SetLocalVariables(uint oObject);
    }
}
