using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.NWNX
{
    public class OnUseSkillBefore : BaseEvent
    {
        public override string Script => ScriptName.OnUseSkillBefore;
    }
}
