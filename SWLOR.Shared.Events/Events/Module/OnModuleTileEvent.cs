using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.Module
{
    public class OnModuleTileEvent : BaseEvent
    {
        public override string Script => ScriptName.OnModuleTileEvent;
    }
}
