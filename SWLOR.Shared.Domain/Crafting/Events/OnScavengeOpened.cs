using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Crafting.Events
{
    public class OnScavengeOpened : BaseEvent
    {
        public override string Script => CraftingScriptName.OnScavengeOpened;
    }
}
