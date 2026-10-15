using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ChatCommandService;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    public class CombatRecorderChatCommand : IChatCommandListDefinition
    {
        private const string Usage = "Usage: /combatrecord start [label] | stop | status";

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            return new ChatCommandBuilder()
                .Create("combatrecord", "crec")
                .Description("Records every combat event in your current area to a server log file. " + Usage)
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .Validate((user, args) =>
                {
                    if (args.Length < 1)
                        return Usage;

                    var subcommand = args[0].ToLowerInvariant();
                    return subcommand is "start" or "stop" or "status"
                        ? string.Empty
                        : Usage;
                })
                .Action((user, target, location, args) =>
                {
                    string message;
                    switch (args[0].ToLowerInvariant())
                    {
                        case "start":
                            var label = string.Join(" ", args, 1, args.Length - 1);
                            CombatRecorder.Start(GetArea(user), label, out message);
                            break;
                        case "stop":
                            CombatRecorder.Stop("dm command", out message);
                            break;
                        default:
                            message = CombatRecorder.GetStatus();
                            break;
                    }

                    SendMessageToPC(user, message);
                })
                .Build();
        }
    }
}
