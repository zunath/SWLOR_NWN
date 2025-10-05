using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.World.Events
{
    public class OnPlaceableTeleport : BaseEvent
    {
        public override string Script => WorldScriptName.OnPlaceableTeleport;
    }
}
