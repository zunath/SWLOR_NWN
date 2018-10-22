using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Switches the active language. Use /language help for more information.", CommandPermissionType.Player | CommandPermissionType.DM)]
    public class Language : IChatCommand
    {
        private readonly ILanguageService _language;
        private readonly IColorTokenService _color;

        public Language(
            ILanguageService language,
            IColorTokenService color)
        {
            _language = language;
            _color = color;
        }

        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            if (args.Length < 1)
            {
                user.SendMessage(_color.Red("Please enter /language help for more information on how to use this command."));
                return;
            }

            string command = args[0].ToLower();

            if (command == "help")
            {
                List<string> commands = new List<string>
                {
                    "help: Displays this help text."
                };

                foreach (SkillType language in _language.GetLanguages())
                {
                    commands.Add($"{language.ToString()}: Sets the active language to {language.ToString()}.");
                }

                user.SendMessage(commands.Aggregate((a, b) => a + '\n' + b));
                return;
            }

            foreach (SkillType language in _language.GetLanguages())
            {
                if (language.ToString().ToLower() == command)
                {
                    _language.SetActiveLanguage(user, language);
                    user.SendMessage($"Set active language to {language.ToString()}.");
                    return;
                }
            }

            user.SendMessage(_color.Red($"Unknown language {command}."));
        }

        public bool RequiresTarget => false;
    }
}