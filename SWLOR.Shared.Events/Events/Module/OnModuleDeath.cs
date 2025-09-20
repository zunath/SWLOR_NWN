using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Module
{
    public class OnModuleDeath : BaseEvent
    {
        public override string ScriptName => "mod_death";
    }
}
