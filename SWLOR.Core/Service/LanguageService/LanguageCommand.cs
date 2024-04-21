using SWLOR.Core.Service.SkillService;

namespace SWLOR.Core.Service.LanguageService
{
    public class LanguageCommand
    {
        public string ProperName { get; }
        public IEnumerable<string> ChatNames { get; }
        public SkillType Skill { get; }

        public LanguageCommand(string properName, SkillType skill, IEnumerable<string> chatNames)
        {
            ProperName = properName;
            Skill = skill;
            ChatNames = chatNames;
        }
    }
}
