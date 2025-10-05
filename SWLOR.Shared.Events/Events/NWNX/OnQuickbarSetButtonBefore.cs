using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.NWNX
{
    public class OnQuickbarSetButtonBefore : BaseEvent
    {
        public override string Script => ScriptName.OnQuickbarSetButtonBefore;
    }
}
