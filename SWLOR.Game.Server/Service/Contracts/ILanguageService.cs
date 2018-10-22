using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface ILanguageService
    {
        string TranslateSnippetForListener(NWObject speaker, NWObject listener, SkillType language, string snippet);
        int GetColour(SkillType language);
        string GetName(SkillType language);
        void InitializePlayerLanguages(NWPlayer player);
        SkillType GetActiveLanguage(NWObject obj);
        void SetActiveLanguage(NWObject obj, SkillType language);
        SkillType[] GetLanguages();
    }
}