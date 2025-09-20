using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Module
{
    public class OnModuleUnacquire : BaseEvent
    {
        public override string ScriptName => "mod_unacquire";
    }
}
