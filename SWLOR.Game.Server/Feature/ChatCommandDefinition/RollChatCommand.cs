using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ChatCommandService;
using SWLOR.Game.Server.Service.DiceService;
using static SWLOR.NWN.API.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    public class RollChatCommand : IChatCommandListDefinition
    {
        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            var builder = new ChatCommandBuilder();

            builder.Create("r", "roll")
                .Description("Rolls dice from a text expression, e.g. /r 1d20+3d6+1d8adv. Supports kh/kl, exploding (!), reroll (rN), multiplier (xN) and adv/dis.")
                .Permissions(AuthorizationLevel.All)
                .Validate((user, args) =>
                {
                    if (args == null || args.Length == 0 || string.IsNullOrWhiteSpace(string.Concat(args)))
                        return "Usage: /r <expression>   e.g. /r 1d20+3d6+1d8adv";

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    // The dispatcher splits on spaces; re-join so "1d20 + 3d6" and "1d20+3d6" behave the same.
                    var expression = string.Concat(args);

                    if (Dice.TryRoll(expression, out var message, out var error))
                    {
                        // Speak on the Talk channel so nearby players see it, matching /dice.
                        AssignCommand(user, () => SpeakString(message));
                    }
                    else
                    {
                        SendMessageToPC(user, ColorToken.Red(error));
                    }
                });

            return builder.Build();
        }
    }
}
