using System.Linq;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Adds a new DM. Arguments order: Name CDKey. Example: /adddm Zunath XXXXYYYY", CommandPermissionType.Admin)]
    public class AddDM: IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            var name = args[0];
            var cdKey = args[1].ToUpper();

            var record = DataService.AuthorizedDM.GetByCDKeyAndActiveOrDefault(cdKey);
            var method = DatabaseActionType.Update;

            if(record == null)
            {
                method = DatabaseActionType.Insert;
                var id = DataService.AuthorizedDM.GetAll().Max(x => x.ID) + 1;
                record = new AuthorizedDM
                {
                    ID = id, 
                    CDKey = cdKey
                };
            }

            record.Name = name;
            record.DMRole = 1;
            record.IsActive = true;

            DataService.SubmitDataChange(record, method);

            user.SendMessage("DM '" + name + "' has been added successfully.");
        }

        public bool RequiresTarget => false;
        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            if (args.Length < 2)
            {
                return "Adding a DM requires a name and a CD key. Example: /adddm Zunath XXXXYYYY";
            }

            if (args.Length > 2)
            {
                return "Too many arguments specified. Names cannot have spaces.";
            }

            var name = args[0];
            var cdKey = args[1].ToUpper();

            if (name.Length > 255)
            {
                return "Names must be 255 characters or less.";
            }

            if (cdKey.Length != 8)
            {
                return "CD Keys must be exactly 8 characters.";
            }

            return string.Empty;
        }
    }
}
