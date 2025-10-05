using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.Area
{
    public class OnAreaExit : BaseEvent
    {
        public override string Script => ScriptName.OnAreaExit;
    }
}
