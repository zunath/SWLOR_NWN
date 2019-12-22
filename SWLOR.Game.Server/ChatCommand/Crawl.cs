using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;
using static NWN._;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Toggles crawling on the ground.", CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class Crawl: IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            var pheno = GetPhenoType(user);

            if (pheno == PhenoType.Large)
            {
                SetPhenoType(PhenoType.Normal, user);
            }
            else
            {
                SetPhenoType(PhenoType.Normal, user);
            }
 
        }

        public bool RequiresTarget => false;
        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return "Sorry but this feature is not yet implemented.";
        }
    }
}
