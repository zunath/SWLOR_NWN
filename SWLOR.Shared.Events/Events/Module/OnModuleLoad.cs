using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Module
{
    public class OnModuleLoad: BaseEvent
    {
        public override string ScriptName => "mod_load";
    }
}
