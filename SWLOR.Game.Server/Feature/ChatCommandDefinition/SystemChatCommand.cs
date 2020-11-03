using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ChatCommandService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    public class SystemChatCommand: IChatCommandListDefinition
    {
        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            var builder = new ChatCommandBuilder();

            BugCommand(builder);
            HelpCommand(builder);

            return builder.Build();
        }

        private static void BugCommand(ChatCommandBuilder builder)
        {
            builder.Create("bug")
                .Description("Report a bug to the developers. Please include as much detail as possible.")
                .Permissions(AuthorizationLevel.All)
                .Validate((user, args) =>
                {
                    if (args.Length <= 0 || args[0].Length <= 0)
                    {
                        return "Please enter in a description for the bug.";
                    }

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    string message = string.Empty;

                    foreach (var arg in args)
                    {
                        message += " " + arg;
                    }

                    if (message.Length > 1000)
                    {
                        SendMessageToPC(user, "Your message was too long. Please shorten it to no longer than 1000 characters and resubmit the bug. For reference, your message was: \"" + message + "\"");
                        return;
                    }
                    var isPlayer = GetIsPC(user);
                    var area = GetArea(user);
                    var areaResref = GetResRef(area);
                    var position = GetPosition(user);
                    var orientation = GetFacing(user);

                    BugReport report = new BugReport
                    {
                        SenderPlayerID = isPlayer ? (Guid?)Guid.Parse(GetObjectUUID(user)) : null,
                        CDKey = GetPCPublicCDKey(user),
                        Text = message,
                        AreaResref = areaResref,
                        SenderLocationX = position.X,
                        SenderLocationY = position.Y,
                        SenderLocationZ = position.X,
                        SenderLocationOrientation = orientation
                    };

                    var key = Guid.NewGuid().ToString();
                    DB.Set(key, report);
                    SendMessageToPC(user, "Bug report submitted! Thank you for your report.");
                });
        }

        private static void HelpCommand(ChatCommandBuilder builder)
        {
            builder.Create("help")
                .Description("Displays all chat commands available to you.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) =>
                {
                    var authorization = Authorization.GetAuthorizationLevel(user);

                    if (authorization == AuthorizationLevel.DM)
                    {
                        SendMessageToPC(user, Service.ChatCommand.HelpTextDM);
                    }
                    else if (authorization == AuthorizationLevel.Admin)
                    {
                        SendMessageToPC(user, Service.ChatCommand.HelpTextAdmin);
                    }
                    else
                    {
                        SendMessageToPC(user, Service.ChatCommand.HelpTextPlayer);
                    }
                });
        }
    }
}
