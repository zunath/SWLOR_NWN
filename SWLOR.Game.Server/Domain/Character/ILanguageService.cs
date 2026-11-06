using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.LanguageService;
using SkillType = SWLOR.Game.Server.Service.SkillService.SkillType;

namespace SWLOR.Game.Server.Domain.Character
{
    public interface ILanguageService
    {
        /// <summary>When the module loads, creates translators for every language and stores them.</summary>
        void LoadTranslators();

        string TranslateSnippetForListener(uint speaker, uint listener, SkillType language, string snippet);

        (byte, byte, byte) GetColor(SkillType language);

        string GetName(SkillType language);

        SkillType GetActiveLanguage(uint obj);

        void SetActiveLanguage(uint obj, SkillType language);

        IEnumerable<LanguageCommand> Languages { get; }

        /// <summary>
        /// Raises a language to its maximum rank.
        /// Languages don't level up like normal skills (no stat increases, SP, etc.),
        /// so it's safe to set the rank directly.
        /// </summary>
        void Master(Player dbPlayer, SkillType language);

        /// <summary>Retrieves the languages a player has not yet mastered, in display order.</summary>
        List<SkillType> GetUnmasteredLanguages(Player dbPlayer);

        /// <summary>
        /// Spends one language token to master a language the player has not mastered yet.
        /// The caller is responsible for saving the player entity.
        /// </summary>
        /// <returns>true if a token was spent, false otherwise.</returns>
        bool TryMasterWithToken(Player dbPlayer, SkillType language);
    }
}
