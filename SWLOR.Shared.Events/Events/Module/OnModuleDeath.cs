using SWLOR.Shared.Events.EventAggregator;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.Module
{
    public class OnModuleDeath : BaseEvent
    {
        public override string Script => ScriptName.OnModuleDeath;
    }
}
