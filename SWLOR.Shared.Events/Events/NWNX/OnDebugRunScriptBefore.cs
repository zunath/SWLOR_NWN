using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.NWNX
{
    public class OnDebugRunScriptBefore : BaseEvent
    {
        public override string Script => ScriptName.OnDebugRunScriptBefore;
    }
}
