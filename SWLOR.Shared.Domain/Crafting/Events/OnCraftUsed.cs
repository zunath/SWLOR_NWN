using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Crafting.Events
{
    public class OnCraftUsed : BaseEvent
    {
        public override string Script => CraftingScriptName.OnCraftUsed;
    }
}
