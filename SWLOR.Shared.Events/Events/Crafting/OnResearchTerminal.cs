using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Crafting
{
    public class OnResearchTerminal : BaseEvent
    {
        public override string Script => ScriptName.OnResearchTerminal;
    }
}
