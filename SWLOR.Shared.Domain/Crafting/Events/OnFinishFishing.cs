using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Crafting.Events
{
    public class OnFinishFishing : BaseEvent
    {
        public override string Script => CraftingScriptName.OnFinishFishing;
    }
}
