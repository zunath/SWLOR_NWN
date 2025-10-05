using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.Eventing
{
    internal class OnEventingModuleLevelUp : BaseEvent
    {
        public override string Script => ScriptName.OnEventingModuleLevelUp;
    }
}
