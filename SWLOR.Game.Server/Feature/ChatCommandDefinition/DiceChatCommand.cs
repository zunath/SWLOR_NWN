using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ChatCommandService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using static SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    public class DiceChatCommand: IChatCommandListDefinition
    {
        private static readonly string _genericErrorText = "Please enter /dice help for more information on how to use this command.";
        private static readonly string _genericError = ColorToken.Red(_genericErrorText);

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            var builder = new ChatCommandBuilder();

            builder.Create("dice")
                .Description(_genericErrorText)
                .Permissions(AuthorizationLevel.All)
                .Validate((user, args) =>
                {
                    if (args.Length < 1)
                    {
                        return _genericError;
                    }

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    var command = args[0].ToLower();

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
                            var sides = Convert.ToInt32(command.Substring(1));
                            var count = 1;
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
                });

            return builder.Build();
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

            var message = string.Join("\n", commands);

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

            var dieRoll = number + "d" + sides;
            var message = ColorToken.SkillCheck("Dice Roll: ") + dieRoll + ": " + value;

            AssignCommand(user, () => SpeakString(message));
        }
    }
}
