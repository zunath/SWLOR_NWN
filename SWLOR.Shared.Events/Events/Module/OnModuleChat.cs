using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Module
{
    public class OnModuleChat : BaseEvent
    {
        public override string ScriptName => "mod_chat";
    }
}
