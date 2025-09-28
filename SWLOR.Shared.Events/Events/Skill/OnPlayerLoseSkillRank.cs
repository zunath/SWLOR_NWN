using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Skill
{
    public class OnPlayerLoseSkillRank : BaseEvent
    {
        public override string Script => ScriptName.OnPlayerLoseSkillRank;
    }
}
