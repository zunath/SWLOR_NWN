using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Gets the list of DMs.", CommandPermissionType.Admin)]
    public class GetDMs : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            var staff = DataService.AuthorizedDM.GetAll();
            string message = string.Empty;

            foreach(var member in staff)
            {
                string role = member.DMRole == 1 ? "DM" : "Admin";
                string active = member.IsActive ? 
                    ColorTokenService.Green(" [ACTIVE]") :
                    ColorTokenService.Red(" [INACTIVE]");

                message += member.ID + ": " + member.Name + " (" + member.CDKey + ") / " + role + active + "\n";
            }

            user.SendMessage(message);
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }

        public bool RequiresTarget => false;
    }
}