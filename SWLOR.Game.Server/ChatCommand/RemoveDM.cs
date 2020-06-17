using System;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.NWN._;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Removes an existing DM by ID. Use /GetDMs to get the ID. Example: /RemoveDM 123. You cannot remove yourself.", CommandPermissionType.Admin)]
    public class RemoveDM : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            var id = Convert.ToInt32(args[0]);
            var record = DataService.AuthorizedDM.GetByID(id);
            var userCDKey = GetPCPublicCDKey(user);

            if(record == null)
            {
                user.SendMessage("Unable to locate an authorized DM with ID #" + id + ". Use the /GetDMs command to find a valid ID.");
                return;
            }

            if(record.CDKey == userCDKey)
            {
                user.SendMessage("You cannot remove yourself from the authorized DM list.");
                return;
            }

            if(!record.IsActive)
            {
                user.SendMessage("That DM has already been deactivated.");
                return;
            }

            record.IsActive = false;
            DataService.SubmitDataChange(record, DatabaseActionType.Update);

            foreach(var dm in AppCache.ConnectedDMs)
            {
                if(GetPCPublicCDKey(dm) == record.CDKey)
                {
                    BootPC(dm, "Your DM authorization has been revoked.");
                }
            }
        }

        public bool RequiresTarget => false;
        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            if (args.Length < 1)
            {
                return "Removing a DM requires an ID. Example: /RemoveDM 123";
            }

            if (args.Length > 1)
            {
                return "Too many arguments specified. Only provide an ID number. Example: /RemoveDM 123";
            }

            string idString = args[0];
            if (!int.TryParse(idString, out var _))
            {
                return "ID must be an integer. Retrieve the ID with the /GetDMs command.";
            }

            return string.Empty;
        }
    }
}
