using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.NWNX
{
    public class OnStealthEnterAfter : BaseEvent
    {
        public override string Script => ScriptName.OnStealthEnterAfter;
    }
}
