using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Crafting.Events
{
    public class OnScavengeDisturbed : BaseEvent
    {
        public override string Script => ScriptName.OnScavengeDisturbed;
    }
}
