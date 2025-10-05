using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.Eventing
{
    internal class OnEventingModulePreload : BaseEvent
    {
        public override string Script => ScriptName.OnEventingModulePreload;
    }
}
