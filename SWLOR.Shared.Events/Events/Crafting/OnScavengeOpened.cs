using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Crafting
{
    public class OnScavengeOpened : BaseEvent
    {
        public override string Script => ScriptName.OnScavengeOpened;
    }
}
