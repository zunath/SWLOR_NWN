using SWLOR.Game.Server.Legacy.ChatCommand.Contracts;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Legacy.ChatCommand
{
    [CommandDetails("Gets the list of DMs.", CommandPermissionType.Admin)]
    public class GetDMs : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            var staff = DataService.AuthorizedDM.GetAll();
            var message = string.Empty;

            foreach(var member in staff)
            {
                var role = member.DMRole == 1 ? "DM" : "Admin";
                var active = member.IsActive ? 
                    ColorToken.Green(" [ACTIVE]") :
                    ColorToken.Red(" [INACTIVE]");

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