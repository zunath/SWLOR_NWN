using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Crafting.Events
{
    public class OnResearchTerminal : BaseEvent
    {
        public override string Script => CraftingScriptName.OnResearchTerminal;
    }
}
