using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Sets the lease bonus or penalty on a particular player. Range must be between -99 and 500.", CommandPermissionType.DM)]
    public class SetLeaseRate : IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            if (!target.IsPlayer)
            {
                user.SendMessage("Only players may be targeted with this command.");
                return;
            }

            NWPlayer player = target.Object;
            var dbPlayer = DataService.Player.GetByID(player.GlobalID);
            int leaseRate = int.Parse(args[0]);
            dbPlayer.LeaseRate = leaseRate;
            DataService.SubmitDataChange(dbPlayer, DatabaseActionType.Update);

            user.SendMessage(player.Name + ": Lease rate set to " + dbPlayer.LeaseRate + "%");

            if (leaseRate == 0)
            {
                player.FloatingText("Your lease rate has returned to normal.");
            }
            else if (leaseRate > 0)
            {
                player.FloatingText("Your lease rate has increased to " + dbPlayer.LeaseRate + "% of normal.");
            }
            else if (leaseRate < 0)
            {
                player.FloatingText("Your lease rate has decreased to " + dbPlayer.LeaseRate + "% of normal.");
            }
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            if (args.Length < 1)
            {
                return "Please enter a value. Example: /setleaserate 5";
            }

            if (!int.TryParse(args[0], out int result))
            {
                return "Invalid number set for command. Values should be between -99 and 500.";
            }

            if (result > 500)
            {
                return "Lease rate penalties cannot be set higher than 500%. Please rerun the command using a number between -99 and 500.";
            }
            else if (result < -99)
            {
                return "Lease rate bonuses cannot be set lower than -99%. Please rerun the command using a number between -99 and 500.";
            }


            return string.Empty;
        }

        public bool RequiresTarget => true;
    }
}
