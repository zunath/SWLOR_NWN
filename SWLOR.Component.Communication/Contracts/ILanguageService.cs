using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Shared.Core.Contracts
{
    public interface ILanguageService
    {
        void LoadTranslators();
        string TranslateSnippetForListener(uint speaker, uint listener, SkillType language, string snippet);
        (byte, byte, byte) GetColor(SkillType language);
        string GetName(SkillType language);
        SkillType GetActiveLanguage(uint obj);
        void SetActiveLanguage(uint obj, SkillType language);
        IEnumerable<LanguageCommand> Languages { get; }
    }
}
