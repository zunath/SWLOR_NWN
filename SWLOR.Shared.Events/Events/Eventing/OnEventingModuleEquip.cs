using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.EventAggregator;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.Eventing
{
    internal class OnEventingModuleEquip : BaseEvent
    {
        public override string Script => ScriptName.OnEventingModuleEquip;
    }
}
