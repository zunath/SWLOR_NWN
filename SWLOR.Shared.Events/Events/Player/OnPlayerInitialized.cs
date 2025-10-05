using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.Player
{
    public class OnPlayerInitialized : BaseEvent
    {
        public override string Script => ScriptName.OnPlayerInitialized;
    }
}
