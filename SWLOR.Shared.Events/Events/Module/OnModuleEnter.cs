using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Module
{
    public class OnModuleEnter : BaseEvent
    {
        public override string ScriptName => "mod_enter";
    }
}
