using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using System;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Report a bug to the developers. Please include as much detail as possible.", CommandPermissionType.Player | CommandPermissionType.DM)]
    public class Bug : IChatCommand
    {
        private readonly INWScript _;
        private readonly IDataContext _db;

        public Bug(
            INWScript script,
            IDataContext db)
        {
            _ = script;
            _db = db;
        }

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

            BugReport report = new BugReport
            {
                SenderPlayerID = user.IsPlayer ? user.GlobalID : null,
                CDKey = _.GetPCPublicCDKey(user),
                Text = message,
                TargetName = target.IsValid ? target.Name : string.Empty,
                AreaResref = user.Area.Resref,
                SenderLocationX = user.Location.X,
                SenderLocationY = user.Location.Y,
                SenderLocationZ = user.Location.Z,
                SenderLocationOrientation = user.Location.Orientation,
                DateSubmitted = DateTime.UtcNow
            };

            _db.BugReports.Add(report);
            _db.SaveChanges();

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

        public bool RequiresTarget => true;
    }
}
