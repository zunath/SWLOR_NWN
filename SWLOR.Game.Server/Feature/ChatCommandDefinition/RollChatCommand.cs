using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ChatCommandService;
using SWLOR.Game.Server.Service.DiceService;
using SWLOR.Game.Server.Service.LogService;
using static SWLOR.NWN.API.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    public class RollChatCommand : IChatCommandListDefinition
    {
        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            var builder = new ChatCommandBuilder();

            builder.Create("r", "roll")
                .Description("Rolls dice from a text expression, e.g. /r 1d20+5 or /r adv. Supports advantage/disadvantage (adv/dis), keep-highest/lowest (kh/kl), exploding (!), reroll (rN) and multiplier (xN).")
                .Permissions(AuthorizationLevel.All)
                .Validate((user, args) =>
                {
                    if (args == null || args.Length == 0 || string.IsNullOrWhiteSpace(string.Concat(args)))
                        return "Usage: /r <expression>   e.g. /r 1d20+5, /r adv, /r 4d6kh3";

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
                        Log.Write(LogGroup.Chat, $"/r parse failed for '{expression}' from {GetName(user)}: {error}");
                        SendMessageToPC(user, ColorToken.Red(error));
                    }
                });

            return builder.Build();
        }
    }
}
