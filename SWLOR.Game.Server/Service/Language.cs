using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.LanguageService;
using SkillType = SWLOR.Game.Server.Service.SkillService.SkillType;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the languages. The logic lives in <see cref="ILanguageService"/>; the module-load handler
    /// stays here and every member forwards to the container's instance.
    /// </summary>
    public static class Language
    {
        private static ILanguageService Instance => FacadeAccessor.Resolve<ILanguageService>();

        /// <summary>
        /// When the module loads, create translators for every language and store them into cache.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void LoadTranslators()
        {
            Instance.LoadTranslators();
        }

        public static string TranslateSnippetForListener(uint speaker, uint listener, SkillType language, string snippet)
        {
            return Instance.TranslateSnippetForListener(speaker, listener, language, snippet);
        }

        public static (byte, byte, byte) GetColor(SkillType language)
        {
            return Instance.GetColor(language);
        }

        public static string GetName(SkillType language)
        {
            return Instance.GetName(language);
        }

        public static SkillType GetActiveLanguage(uint obj)
        {
            return Instance.GetActiveLanguage(obj);
        }

        public static void SetActiveLanguage(uint obj, SkillType language)
        {
            Instance.SetActiveLanguage(obj, language);
        }

        public static IEnumerable<LanguageCommand> Languages => Instance.Languages;

        /// <summary>
        /// Raises a language to its maximum rank.
        /// Languages don't level up like normal skills (no stat increases, SP, etc.),
        /// so it's safe to set the rank directly.
        /// </summary>
        /// <param name="dbPlayer">The player entity.</param>
        /// <param name="language">The language to master.</param>
        public static void Master(Player dbPlayer, SkillType language)
        {
            Instance.Master(dbPlayer, language);
        }

        /// <summary>
        /// Retrieves the languages a player has not yet mastered, in display order.
        /// </summary>
        /// <param name="dbPlayer">The player entity.</param>
        /// <returns>The languages below their maximum rank.</returns>
        public static List<SkillType> GetUnmasteredLanguages(Player dbPlayer)
        {
            return Instance.GetUnmasteredLanguages(dbPlayer);
        }

        /// <summary>
        /// Spends one language token to master a language the player has not mastered yet.
        /// The caller is responsible for saving the player entity.
        /// </summary>
        /// <param name="dbPlayer">The player entity.</param>
        /// <param name="language">The language to master.</param>
        /// <returns>true if a token was spent, false otherwise.</returns>
        public static bool TryMasterWithToken(Player dbPlayer, SkillType language)
        {
            return Instance.TryMasterWithToken(dbPlayer, language);
        }
    }
}
