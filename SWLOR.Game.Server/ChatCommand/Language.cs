using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Switches the active language. Use /language help for more information.", CommandPermissionType.Player | CommandPermissionType.DM)]
    public class Language : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            string command = args[0].ToLower();
            CustomRaceType race = (CustomRaceType)user.RacialType;

            if (command == "help")
            {
                List<string> commands = new List<string>
                {
                    "help: Displays this help text."
                };

                foreach (SkillType language in LanguageService.GetLanguages())
                {
                    commands.Add($"{language.ToString()}: Sets the active language to {language.ToString()}.");
                }

                user.SendMessage(commands.Aggregate((a, b) => a + '\n' + b));
                return;
            }

            // Wookiees cannot speak any language besides Shyriiwook.
            if (race == CustomRaceType.Wookiee && 
                command != SkillType.Shyriiwook.ToString().ToLower())
            {
                LanguageService.SetActiveLanguage(user, SkillType.Shyriiwook);
                user.SendMessage(ColorTokenService.Red("Wookiees can only speak Shyriiwook."));
                return;
            }

            foreach (SkillType language in LanguageService.GetLanguages())
            {
                if (language.ToString().ToLower() == command)
                {
                    LanguageService.SetActiveLanguage(user, language);
                    user.SendMessage($"Set active language to {language.ToString()}.");
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