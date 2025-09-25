using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Domain.AI.ValueObjects;

namespace SWLOR.Shared.Domain.Character.Contracts
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
