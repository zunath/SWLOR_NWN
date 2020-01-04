using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Language
{
    public class LanguageCommand
    {
        public string ProperName { get; }
        public IEnumerable<string> ChatNames { get; }
        public Skill Skill { get; }

        public LanguageCommand(string properName, Skill skill, IEnumerable<string> chatNames)
        {
            ProperName = properName;
            Skill = skill;
            ChatNames = chatNames;
        }
    }
}
