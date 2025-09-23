using SWLOR.Component.Communication.Model;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.Communication.Contracts
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
