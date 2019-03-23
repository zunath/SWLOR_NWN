using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Spawns gold of a specific quantity on your character. Example: /spawngold 33", CommandPermissionType.DM)]
    public class SpawnGold : IChatCommand
    {
        /// <summary>
        /// Spawns an item by resref in the user's inventory.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="target"></param>
        /// <param name="targetLocation"></param>
        /// <param name="args"></param>
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            int quantity = 1;

            if (args.Length >= 1)
            {
                if (!int.TryParse(args[0], out quantity))
                {
                    return;
                }
            }

            _.GiveGoldToCreature(user, quantity);
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            if (args.Length <= 0)
            {
                return ColorTokenService.Red("Please specify a quantity. Example: /" + nameof(SpawnGold) + " 34");
            }
            return string.Empty;
        }

        public bool RequiresTarget => false;
    }
}
