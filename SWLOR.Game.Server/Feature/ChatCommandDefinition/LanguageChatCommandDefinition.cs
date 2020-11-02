using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    public class LanguageChatCommandDefinition : ChatCommand.ChatCommandDefinition
    {
        public LanguageChatCommandDefinition()
            : base(
                "Switches the active language. Use /language help for more information.",
                ChatCommand.CommandPermissionType.Player | ChatCommand.CommandPermissionType.DM |
                ChatCommand.CommandPermissionType.Admin,
                HandleAction,
                HandleArgumentValidation,
                false)
        {
        }

        private static string HandleArgumentValidation(uint user, params string[] args)
        {
            if (args.Length < 1)
            {
                return ColorToken.Red("Please enter /language help for more information on how to use this command.");
            }

            return string.Empty;
        }

        private static void HandleAction(uint user, uint target, Location targetLocation, params string[] args)
        {
            var command = args[0].ToLower();
            var race = GetRacialType(user);
            var languages = Language.Languages;

            if (command == "help")
            {
                var commands = new List<string>
                {
                    "help: Displays this help text."
                };

                foreach (var language in languages)
                {
                    var chatText = language.ChatNames.ElementAt(0);
                    var count = language.ChatNames.Count();

                    for (var x = 1; x < count; x++)
                    {
                        chatText += ", " + language.ChatNames.ElementAt(x);
                    }

                    commands.Add($"{chatText}: Sets the active language to {language.ProperName}.");
                }

                SendMessageToPC(user, commands.Aggregate((a, b) => a + '\n' + b));
                return;
            }

            // Wookiees cannot speak any language besides Shyriiwook.
            if (race == RacialType.Wookiee &&
                command != SkillType.Shyriiwook.ToString().ToLower())
            {
                Language.SetActiveLanguage(user, SkillType.Shyriiwook);
                SendMessageToPC(user, ColorToken.Red("Wookiees can only speak Shyriiwook."));
                return;
            }


            foreach (var language in Language.Languages)
            {
                if (language.ChatNames.Contains(command))
                {
                    Language.SetActiveLanguage(user, language.Skill);
                    SendMessageToPC(user, $"Set active language to {language.ProperName}.");
                    return;
                }
            }

            SendMessageToPC(user, ColorToken.Red($"Unknown language {command}."));
        }
    }
}
