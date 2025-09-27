using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.ValueObjects;

namespace SWLOR.Shared.Domain.Character.Contracts
{
    public interface IAnimator
    {
        VisualEffectType Vfx { get; set; }

        AnimationEvent Event { get; set; }

        DurationType Duration { get; set; }

        public float Scale { get; set; }

        public void SetLocalVariables(uint oObject);
    }
}
