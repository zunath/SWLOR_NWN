using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Module
{
    public class OnModuleExit : BaseEvent
    {
        public override string ScriptName => "mod_exit";
    }
}
