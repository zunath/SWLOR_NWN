using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Crafting.Events
{
    public class OnFishPoint : BaseEvent
    {
        public override string Script => CraftingScriptName.OnFishPoint;
    }
}
