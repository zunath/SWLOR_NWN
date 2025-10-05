using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.World.Events
{
    public class OnPlaceableBuyRebuild : BaseEvent
    {
        public override string Script => WorldScriptName.OnPlaceableBuyRebuild;
    }
}
