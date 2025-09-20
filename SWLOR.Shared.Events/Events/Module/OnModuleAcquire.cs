using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Module
{
    public class OnModuleAcquire : BaseEvent
    {
        public override string ScriptName => "mod_acquire";
    }
}
