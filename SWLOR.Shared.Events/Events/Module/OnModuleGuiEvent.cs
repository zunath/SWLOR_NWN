using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Module
{
    public class OnModuleGuiEvent : BaseEvent
    {
        public override string ScriptName => "mod_guievent";
    }
}
