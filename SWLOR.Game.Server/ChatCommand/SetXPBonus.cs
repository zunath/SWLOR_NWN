using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Sets the XP bonus on a particular player.", CommandPermissionType.DM)]
    public class SetXPBonus : IChatCommand
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
            int xpBonus = int.Parse(args[0]);
            dbPlayer.XPBonus = xpBonus;
            DataService.SubmitDataChange(dbPlayer, DatabaseActionType.Update);

            user.SendMessage(player.Name + ": XP Bonus set to " + dbPlayer.XPBonus);
            player.FloatingText("You have received a permanent +" + xpBonus + "% XP increase from a DM!");
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            if (args.Length < 1)
            {
                return "Please enter a value. Example: /setxpbonus 5";
            }

            if (!int.TryParse(args[0], out int result))
            {
                return "Invalid number set for command. Values should be between 0-25.";
            }

            if(result > 25)
            {
                return "Bonuses cannot be set higher than 25%. Please rerun the command using a number between 0-25.";
            }
            else if (result < 0)
            {
                return "Bonuses cannot be set lower than 0%. Please rerun the command using a number between 0-25.";
            }


            return string.Empty;
        }

        public bool RequiresTarget => true;
    }
}
