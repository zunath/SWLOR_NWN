using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Associate.Events
{
    public class OnDNAExtractUsed : BaseEvent
    {
        public override string Script => ScriptName.OnDNAExtractUsed;
    }
}
