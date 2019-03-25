using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Spawns an item of a specific quantity on your character. Example: /spawnitem my_item 3", CommandPermissionType.DM)]
    public class SpawnItem: IChatCommand
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
            string resref = args[0];
            int quantity = 1;

            if (args.Length > 1)
            {
                if (!int.TryParse(args[1], out quantity))
                {
                    return;
                }
            }

            NWItem item = (_.CreateItemOnObject(resref, user.Object, quantity));

            if (!item.IsValid)
            {
                user.SendMessage(ColorTokenService.Red("Item not found! Did you enter the correct ResRef?"));
                return;
            }

            item.IsIdentified = true;
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            if (args.Length <= 0)
            {
                return ColorTokenService.Red("Please specify a resref and optionally a quantity. Example: /" + nameof(SpawnItem) + " my_resref 20");
            }

            return string.Empty;
        }

        public bool RequiresTarget => false;
    }
}
