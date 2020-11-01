using System.Collections.Generic;
using SkillType = SWLOR.Game.Server.Legacy.Enumeration.SkillType;

namespace SWLOR.Game.Server.Legacy.Language
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
