using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.NWNX
{
    public class OnStealthExitBefore : BaseEvent
    {
        public override string Script => ScriptName.OnStealthExitBefore;
    }
}
