using NWN;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Spawns an item of a specific quantity on your character. Example: /spawnitem my_item 3", CommandPermissionType.DM)]
    public class SpawnItem: IChatCommand
    {
        private readonly INWScript _;
        private readonly IColorTokenService _color;

        public SpawnItem(
            INWScript script,
            IColorTokenService color)
        {
            _ = script;
            _color = color;
        }

        /// <summary>
        /// Spawns an item by resref in the user's inventory.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="target"></param>
        /// <param name="targetLocation"></param>
        /// <param name="args"></param>
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            if (args.Length <= 0)
            {
                user.SendMessage(_color.Red("Please specify a resref and optionally a quantity. Example: /" + nameof(SpawnItem) + " my_resref 20"));
                return;
            }
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
                user.SendMessage(_color.Red("Item not found! Did you enter the correct ResRef?"));
                return;
            }

            item.IsIdentified = true;
        }

        public bool RequiresTarget => false;
    }
}
