using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.NWNX
{
    public class OnCalendarMonth : BaseEvent
    {
        public override string Script => ScriptName.OnCalendarMonth;
    }
}
