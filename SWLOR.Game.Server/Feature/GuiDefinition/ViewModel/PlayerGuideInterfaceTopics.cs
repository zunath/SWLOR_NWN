using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>The Player Guide topics of the Interface category, one method per topic.</summary>
    internal static class PlayerGuideInterfaceTopics
    {
        internal static PlayerGuideTopic CreateUsefulWindows()
        {
            return new(
                "Useful Windows",
                "Interface",
                "The main windows and shortcuts new players commonly need.",
                "Guide and sheet",
                new[]
                {
                    new ArticleBlock("Player Guide",
                        "Press B to open this Player Guide. Search checks topic names, summaries, article text, and quick answers. Clear the search to browse everything, and use Related Topics to jump between connected systems."),
                    new ArticleBlock("Character Sheet",
                        "Press C to open the Character Sheet. It shows your SP, AP, attributes, combat values, and character actions."),
                    new ArticleBlock("Guide Button",
                        "The Character Sheet also has a Guide button, so you can return here while reviewing your character."),
                    new ArticleBlock("Character Sheet Actions",
                        "The Character Sheet action list opens Skills, Perks, Techniques, Appearance, Disguises, Quests, Open Trash, Recipes, Currencies, Key Items, Notes, this Guide, Achievements, and Settings."),
                    new ArticleBlock("Skills and Perks",
                        "Skills shows ranks, XP, XP debt, decay locks, rank limits, and RP XP distribution. Perks shows player perks and, when you have an active beast, beast perks."),
                    new ArticleBlock("Quests and Progress",
                        "Press J to open Quests, which shows active quest objectives. Key Items shows character-specific unlocks and progress records. Achievements shows long-term account-wide progress."),
                    new ArticleBlock("Social Windows",
                        "Notes stores private notes. Settings controls notifications, subdual mode, reset reminders, Mini-Vitals, identity/privacy options, chat colors, and description access."),
                    new ArticleBlock("Recovery and Utility",
                        "Open Trash provides a disposal container. Appearance and Disguises manage your visible presentation. If a window becomes unreachable, use /resetwindows. Use /help for the full command list and /bug to report a problem.")
                },
                new[]
                {
                    new QuestionAnswer("How do I open the guide?", "Press B."),
                    new QuestionAnswer("How do I open my character sheet?", "Press C."),
                    new QuestionAnswer("Where is the guide button?", "Open the Character Sheet and use the Guide button near the lower part of the action list."),
                    new QuestionAnswer("Where are currencies?", "Open the Character Sheet and choose Currencies."),
                    new QuestionAnswer("Where are quests and key items?", "Open the Character Sheet and choose Quests or Key Items."),
                    new QuestionAnswer("How do I recover a lost window?", "Use /resetwindows to restore saved window positions.")
                },
                new[] { "Common Questions", "Skills", "Perks", "Mimicry & Techniques", "Training Store", "Quests & Key Items", "Communication", "Disguises" });
        }
    }
}
