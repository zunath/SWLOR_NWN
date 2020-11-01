using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    public class DiceChatCommandDefinition : ChatCommand.ChatCommandDefinition
    {
        private static readonly string _genericError = ColorToken.Red("Please enter /dice help for more information on how to use this command.");

        public DiceChatCommandDefinition()
            : base(
            "Rolls dice. Use /dice help for more information.",
            ChatCommand.CommandPermissionType.Player | ChatCommand.CommandPermissionType.DM | ChatCommand.CommandPermissionType.Admin,
            HandleAction,
            HandleArgumentValidation,
            false)
        {
        }

        private static string HandleArgumentValidation(uint user, params string[] args)
        {
            if (args.Length < 1)
            {
                return _genericError;
            }

            return string.Empty;
        }

        private static void HandleAction(uint user, uint target, Location targetLocation, params string[] args)
        {
            string command = args[0].ToLower();

            switch (command)
            {
                case "help":
                    DoHelp(user);
                    break;
                case "d2":
                case "d4":
                case "d6":
                case "d8":
                case "d10":
                case "d20":
                case "d100":
                    int sides = Convert.ToInt32(command.Substring(1));
                    int count = 1;
                    if (args.Length > 1)
                    {
                        if (!int.TryParse(args[1], out count))
                        {
                            count = 1;
                        }
                    }

                    DoDiceRoll(user, sides, count);
                    break;

                default:
                    SendMessageToPC(user, _genericError);
                    return;
            }

        }

        private static void DoHelp(uint user)
        {
            string[] commands =
            {
                "help: Displays this help text.",
                "d2: Rolls a 2-sided die. Follow with a number between 1-10 to roll multiple dice. Example: /dice d2 4",
                "d4: Rolls a 4-sided die. Follow with a number between 1-10 to roll multiple dice. Example: /dice d4 4",
                "d6: Rolls a 6-sided die. Follow with a number between 1-10 to roll multiple dice. Example: /dice d6 4",
                "d8: Rolls an 8-sided die. Follow with a number between 1-10 to roll multiple dice. Example: /dice d8 4",
                "d10: Rolls a 10-sided die. Follow with a number between 1-10 to roll multiple dice. Example: /dice d10 4",
                "d20: Rolls a 20-sided die. Follow with a number between 1-10 to roll multiple dice. Example: /dice d20 4" ,
                "d100: Rolls a 100-sided die. Follow with a number between 1-10 to roll multiple dice. Example: /dice d100 4",

            };

            string message = string.Join("\n", commands);

            SendMessageToPC(user, message);
        }

        private static void DoDiceRoll(uint user, int sides, int number)
        {
            int value;

            if (number < 1)
                number = 1;
            else if (number > 10)
                number = 10;

            switch (sides)
            {
                case 2:
                    value = D2(number);
                    break;
                case 4:
                    value = D4(number);
                    break;
                case 6:
                    value = D6(number);
                    break;
                case 8:
                    value = D8(number);
                    break;
                case 10:
                    value = D10(number);
                    break;
                case 20:
                    value = D20(number);
                    break;
                case 100:
                    value = D100(number);
                    break;
                default:
                    value = 0;
                    break;
            }

            string dieRoll = number + "d" + sides;
            string message = ColorToken.SkillCheck("Dice Roll: ") + dieRoll + ": " + value;

            AssignCommand(user, () => SpeakString(message));
        }
    }
}
