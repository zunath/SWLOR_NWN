using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using System;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Logging;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Report a bug to the developers. Please include as much detail as possible.", CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class Bug : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            string message = string.Empty;

            foreach (var arg in args)
            {
                message += " " + arg;
            }

            if(message.Length > 1000)
            {
                user.SendMessage("Your message was too long. Please shorten it to no longer than 1000 characters and resubmit the bug. For reference, your message was: \"" + message + "\"");
                return;
            }

            var senderPlayerID = user.IsPlayer ? user.GlobalID.ToString() : "No Player ID";
            var targetName = target.IsValid ? target.Name : user.Name;
            Audit.Write(AuditGroup.BugReport, $"REPORT - {senderPlayerID} - {_.GetPCPublicCDKey(user)} - {message} - {targetName} - {user.Area.Resref} - {user.Location.X}, {user.Location.Y}, {user.Location.Z} - {user.Location.Orientation}");

            user.SendMessage("Bug report submitted! Thank you for your report.");
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            if (args.Length <= 0 || args[0].Length <= 0)
            {
                return "Please enter in a description for the bug.";
            }

            return string.Empty;
        }

        public bool RequiresTarget => false;
    }
}
