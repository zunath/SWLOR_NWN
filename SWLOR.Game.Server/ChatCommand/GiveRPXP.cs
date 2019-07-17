using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Gives Roleplay XP to a target player.", CommandPermissionType.DM)]
    public class GiveRPXP: IChatCommand
    {
        private const int MaxAmount = 10000;

        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            if (!target.IsPlayer)
            {
                user.SendMessage("Only players may be targeted with this command.");
                return;
            }

            int amount = int.Parse(args[0]);
            Player dbPlayer = DataService.Player.GetByID(target.GlobalID);
            dbPlayer.RoleplayXP += amount;
            DataService.SubmitDataChange(dbPlayer, DatabaseActionType.Update);
            _.SendMessageToPC(target, "A DM has awarded you with " + amount + " roleplay XP.");
        }

        public bool RequiresTarget => true;
        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            // Missing an amount argument?
            if (args.Length <= 0)
            {
                return "Please specify an amount of RP XP to give. Valid range: 1-" + MaxAmount;
            }

            // Can't parse the amount?
            if(!int.TryParse(args[0], out int amount))
            {
                return "Please specify a valid amount between 1 and " + MaxAmount + ".";
            }

            // Amount is outside of our allowed range?
            if (amount < 1 || amount > MaxAmount)
            {
                return "Please specify a valid amount between 1 and " + MaxAmount + ".";
            }

            return null;
        }
    }
}
