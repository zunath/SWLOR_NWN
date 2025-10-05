using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Skill.Events
{
    public class OnSkillDataCached : BaseEvent
    {
        public override string Script => ScriptName.OnSkillDataCached;
    }
}
