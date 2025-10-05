using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Properties.Events
{
    public class OnOpenPropertyBank : BaseEvent
    {
        public override string Script => ScriptName.OnOpenPropertyBank;
    }
}
