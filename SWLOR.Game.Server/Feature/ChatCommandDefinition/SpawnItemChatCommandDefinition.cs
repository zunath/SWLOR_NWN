using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    public class SpawnItemChatCommandDefinition : ChatCommand.ChatCommandDefinition
    {
        public SpawnItemChatCommandDefinition()
            : base(
                "Spawns an item of a specific quantity on your character. Example: /spawnitem my_item 3",
                ChatCommand.CommandPermissionType.DM | ChatCommand.CommandPermissionType.Admin,
                HandleAction,
                HandleArgumentValidation,
                true)
        {
        }

        private static string HandleArgumentValidation(uint user, params string[] args)
        {
            if (args.Length <= 0)
            {
                return ColorToken.Red("Please specify a resref and optionally a quantity. Example: /spawnitem my_resref 20");
            }

            return string.Empty;
        }

        private static void HandleAction(uint user, uint target, Location targetLocation, params string[] args)
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

            uint item = (CreateItemOnObject(resref, user, quantity));

            if (!GetIsObjectValid(item))
            {
                SendMessageToPC(user, ColorToken.Red("Item not found! Did you enter the correct ResRef?"));
                return;
            }

            SetIdentified(item, true);
        }
    }
}