using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Skill.Events
{
    public class OnPlayerGainSkillRank : BaseEvent
    {
        public override string Script => SkillScriptName.OnPlayerGainSkillRank;
    }
}
