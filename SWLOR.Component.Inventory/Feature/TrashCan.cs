using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Inventory.Events;
using SWLOR.Shared.Events.Events.NWNX;
using SWLOR.Shared.UI.Service;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.Inventory.Feature
{
    public class TrashCan
    {
        private readonly IEventsPluginService _eventsPlugin;

        public TrashCan(IEventsPluginService eventsPlugin)
        {
            _eventsPlugin = eventsPlugin;
        }

        /// <summary>
        /// When a player attempts to drop an item, prevent them from doing so and send a message to use the trash can.
        /// DMs are exempt from this rule.
        /// </summary>
        public void PreventItemDrops()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            _eventsPlugin.SkipEvent();
            
            SendMessageToPC(player, ColorToken.Red("Please use the trash can option in your character menu to discard items."));
        }

        /// <summary>
        /// When the trash can is opened, the player is notified anything placed inside will be destroyed.
        /// </summary>
        public void AlertPlayer()
        {
            var player = GetLastOpenedBy();
            FloatingTextStringOnCreature("Any item placed inside this trash can will be destroyed permanently.", player, false);
        }

        /// <summary>
        /// When the trash can is closed, any items inside will be destroyed and then the placeable will be destroyed.
        /// </summary>
        public void CleanUp()
        {
            var container = OBJECT_SELF;
            for (var item = GetFirstItemInInventory(container); GetIsObjectValid(item); item = GetNextItemInInventory(container))
            {
                DestroyObject(item);
            }

            DestroyObject(container);
        }

        /// <summary>
        /// When an item is added to the trash can, it will be destroyed.
        /// </summary>
        public void DestroyItem()
        {
            var item = GetInventoryDisturbItem();
            var type = GetInventoryDisturbType();

            if (type != DisturbType.Added) return;

            DestroyObject(item);
        }
    }
}
