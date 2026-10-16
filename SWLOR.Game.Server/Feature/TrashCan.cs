using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature
{
    public static class TrashCan
    {
        private const string OwnerVariable = "TRASH_OWNER";

        /// <summary>
        /// When a player attempts to drop an item, prevent them from doing so and send a message to use the trash can.
        /// DMs are exempt from this rule.
        /// </summary>
        [NWNEventHandler(ScriptName.OnInputDropItemBefore)]
        public static void PreventItemDrops()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            EventsPlugin.SkipEvent();

            SendMessageToPC(player, ColorToken.Red("Please use the trash can option in your character menu to discard items."));
        }

        /// <summary>
        /// When the trash can is opened, the player is notified anything placed inside will be destroyed.
        /// </summary>
        [NWNEventHandler(ScriptName.OnTrashOpened)]
        public static void AlertPlayer()
        {
            var player = GetLastOpenedBy();
            SetLocalObject(OBJECT_SELF, OwnerVariable, player);
            FloatingTextStringOnCreature("Any item placed inside this trash can will be destroyed permanently.", player, false);
        }

        /// <summary>
        /// When the trash can is closed, any items inside will be destroyed and then the placeable will be destroyed.
        /// </summary>
        [NWNEventHandler(ScriptName.OnTrashClosed)]
        public static void CleanUp()
        {
            var container = OBJECT_SELF;
            var player = GetLastClosedBy();
            if (!GetIsObjectValid(player))
                player = GetLocalObject(container, OwnerVariable);

            for (var item = GetFirstItemInInventory(container); GetIsObjectValid(item); item = GetNextItemInInventory(container))
            {
                ItemTrash.Audit(player, item, "trash-close");
                DestroyObject(item);
            }

            DestroyObject(container);
        }

        /// <summary>
        /// When an item is added to the trash can, it will be destroyed.
        /// </summary>
        [NWNEventHandler(ScriptName.OnTrashDisturbed)]
        public static void DestroyItem()
        {
            var item = GetInventoryDisturbItem();
            var type = GetInventoryDisturbType();

            if (type != DisturbType.Added) return;

            var player = GetLastDisturbed();
            if (!GetIsObjectValid(player))
                player = GetLocalObject(OBJECT_SELF, OwnerVariable);

            ItemTrash.Audit(player, item, "trash-added");
            DestroyObject(item);
        }
    }
}
