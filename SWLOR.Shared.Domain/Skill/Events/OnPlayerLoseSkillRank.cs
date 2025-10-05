using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Skill.Events
{
    public class OnPlayerLoseSkillRank : BaseEvent
    {
        public override string Script => ScriptName.OnPlayerLoseSkillRank;
    }
}
