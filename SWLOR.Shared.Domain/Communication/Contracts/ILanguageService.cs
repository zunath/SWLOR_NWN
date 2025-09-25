using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Communication.ValueObjects;

namespace SWLOR.Shared.Domain.Communication.Contracts
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
