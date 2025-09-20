using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Module
{
    public class OnModuleCacheBefore : BaseEvent
    {
        public override string ScriptName => "mod_cache_bef";
    }
}
