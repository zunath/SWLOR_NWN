using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Properties.Events
{
    public class OnEnterProperty : BaseEvent
    {
        public override string Script => PropertiesScriptName.OnEnterProperty;
    }
}
