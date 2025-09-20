using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Module
{
    public class OnModuleContentChange : BaseEvent
    {
        public override string ScriptName => "mod_content";
    }
}
