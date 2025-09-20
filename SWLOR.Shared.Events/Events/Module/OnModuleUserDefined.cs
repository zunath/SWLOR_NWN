using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Module
{
    public class OnModuleUserDefined : BaseEvent
    {
        public override string ScriptName => "mod_userdef";
    }
}
