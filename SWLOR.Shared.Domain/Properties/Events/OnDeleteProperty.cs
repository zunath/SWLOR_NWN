using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Properties.Events
{
    public class OnDeleteProperty : BaseEvent
    {
        public override string Script => PropertiesScriptName.OnDeleteProperty;
    }
}
