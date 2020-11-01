using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    public class BugReportChatCommandDefinition: ChatCommand.ChatCommandDefinition
    {
        public BugReportChatCommandDefinition()
            : base(
            "Report a bug to the developers. Please include as much detail as possible.", 
            ChatCommand.CommandPermissionType.Player | ChatCommand.CommandPermissionType.DM | ChatCommand.CommandPermissionType.Admin, 
            HandleAction,
            HandleArgumentValidation, 
            false)
        {
        }

        private static string HandleArgumentValidation(uint user, params string[] args)
        {
            if (args.Length <= 0 || args[0].Length <= 0)
            {
                return "Please enter in a description for the bug.";
            }

            return string.Empty;
        }

        private static void HandleAction(uint user, uint target, Location targetLocation, params string[] args)
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
        }
    }
}
