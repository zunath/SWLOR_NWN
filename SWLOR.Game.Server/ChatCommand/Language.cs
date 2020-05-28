using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.NWN.Enum;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Switches the active language. Use /language help for more information.", CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class Language : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            string command = args[0].ToLower();
            RacialType race = (RacialType)user.RacialType;
            var languages = LanguageService.Languages;

            if (command == "help")
            {
                List<string> commands = new List<string>
                {
                    "help: Displays this help text."
                };

                foreach (var language in languages)
                {
                    var chatText = language.ChatNames.ElementAt(0);
                    int count = language.ChatNames.Count();

                    for (int x = 1; x < count; x++)
                    {
                        chatText += ", " + language.ChatNames.ElementAt(x);
                    }

                    commands.Add($"{chatText}: Sets the active language to {language.ProperName}.");
                }

                user.SendMessage(commands.Aggregate((a, b) => a + '\n' + b));
                return;
            }

            // Wookiees cannot speak any language besides Shyriiwook.
            if (race == RacialType.Wookiee && 
                command != SkillType.Shyriiwook.ToString().ToLower())
            {
                LanguageService.SetActiveLanguage(user, SkillType.Shyriiwook);
                user.SendMessage(ColorTokenService.Red("Wookiees can only speak Shyriiwook."));
                return;
            }


            foreach (var language in LanguageService.Languages)
            {
                if (language.ChatNames.Contains(command))
                {
                    LanguageService.SetActiveLanguage(user, language.Skill);
                    user.SendMessage($"Set active language to {language.ProperName}.");
                    return;
                }
            }

            user.SendMessage(ColorTokenService.Red($"Unknown language {command}."));
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            if (args.Length < 1)
            {
                return ColorTokenService.Red("Please enter /language help for more information on how to use this command.");
            }

            return string.Empty;
        }

        public bool RequiresTarget => false;
    }
}