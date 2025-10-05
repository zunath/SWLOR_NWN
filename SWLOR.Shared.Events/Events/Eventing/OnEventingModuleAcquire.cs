using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.Eventing
{
    internal class OnEventingModuleAcquire : BaseEvent
    {
        public override string Script => ScriptName.OnEventingModuleAcquire;
    }
}
