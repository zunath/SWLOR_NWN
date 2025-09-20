using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Module
{
    public class OnModuleRespawn : BaseEvent
    {
        public override string ScriptName => "mod_respawn";
    }
}
