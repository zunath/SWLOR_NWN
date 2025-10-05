using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.World.Events
{
    public class OnPlaceableSit : BaseEvent
    {
        public override string Script => ScriptName.OnPlaceableSit;
    }
}
