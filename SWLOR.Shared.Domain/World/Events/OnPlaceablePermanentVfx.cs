using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.World.Events
{
    public class OnPlaceablePermanentVfx : BaseEvent
    {
        public override string Script => WorldScriptName.OnPlaceablePermanentVfx;
    }
}
