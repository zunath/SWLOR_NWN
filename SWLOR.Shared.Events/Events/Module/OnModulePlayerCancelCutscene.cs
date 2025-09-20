using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Module
{
    public class OnModulePlayerCancelCutscene : BaseEvent
    {
        public override string ScriptName => "mod_cancel";
    }
}
