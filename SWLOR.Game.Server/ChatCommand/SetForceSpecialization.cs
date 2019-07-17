using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Sets the Force Specialization of the target player. 0 = None, 1 = Guardian, 2 = Consular, 3 = Sentinel.", CommandPermissionType.DM)]
    public class SetForceSpecialization: IChatCommand
    {
        private const int MaxAmount = 3;

        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            if (!target.IsPlayer)
            {
                user.SendMessage("Only players may be targeted with this command.");
                return;
            }

            int type = int.Parse(args[0]);
            Player dbPlayer = DataService.Player.GetByID(target.GlobalID);
            dbPlayer.SpecializationID = (SpecializationType) type;
            DataService.SubmitDataChange(dbPlayer, DatabaseActionType.Update);
            _.SendMessageToPC(target, "A DM has set your Force Specialization type to " + type);
        }

        public bool RequiresTarget => true;
        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            // Missing an amount argument?
            if (args.Length <= 0)
            {
                return "Please specify the Force Specialization to to on the target. 0 = None, 1 = Guardian, 2 = Consular, 3 = Sentinel." + MaxAmount;
            }

            // Can't parse the amount?
            if(!int.TryParse(args[0], out int amount))
            {
                return "Please specify a valid valid parameter. 0 = None, 1 = Guardian, 2 = Consular, 3 = Sentinel.";
            }

            // Amount is outside of our allowed range?
            if (amount < 0 || amount > MaxAmount)
            {
                return "Please specify a valid parameter. 0 = None, 1 = Guardian, 2 = Consular, 3 = Sentinel.";
            }

            return null;
        }
    }
}
