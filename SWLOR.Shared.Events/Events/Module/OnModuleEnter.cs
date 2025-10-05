using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.Module
{
    public class OnModuleEnter : BaseEvent
    {
        public override string Script => ScriptName.OnModuleEnter;
    }
}
