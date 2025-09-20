using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Module
{
    public class OnModuleActivate : BaseEvent
    {
        public override string ScriptName => "mod_activate";
    }
}
