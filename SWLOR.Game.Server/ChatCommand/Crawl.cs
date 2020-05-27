using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using static SWLOR.Game.Server.NWN._;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Toggles crawling on the ground.", CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class Crawl: IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            int pheno = GetPhenoType(user);

            if (pheno == 1)
            {
                SetPhenoType(PHENOTYPE_NORMAL, user);
            }
            else
            {
                SetPhenoType(PHENOTYPE_NORMAL, user);
            }
 
        }

        public bool RequiresTarget => false;
        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return "Sorry but this feature is not yet implemented.";
        }
    }
}
