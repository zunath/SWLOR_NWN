using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Module
{
    public class OnModuleLevelUp : BaseEvent
    {
        public override string ScriptName => "mod_levelup";
    }
}
