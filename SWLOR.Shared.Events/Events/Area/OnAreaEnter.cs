using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.Area
{
    public class OnAreaEnter : BaseEvent
    {
        public override string Script => ScriptName.OnAreaEnter;
    }
}
