using System.Linq;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Placeable.AtomicReassembler
{
    public class OnDisturbed: IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IDialogService _dialog;
        private readonly ICraftService _craft;
        private readonly ISerializationService _serialization;
        private readonly IItemService _item;

        public OnDisturbed(
            INWScript script, 
            IDialogService dialog,
            ICraftService craft,
            ISerializationService serialization,
            IItemService item)
        {
            _ = script;
            _dialog = dialog;
            _craft = craft;
            _serialization = serialization;
            _item = item;
        }

        public bool Run(params object[] args)
        {
            if (_.GetInventoryDisturbType() != INVENTORY_DISTURB_TYPE_ADDED)
                return false;
            
            NWPlayer player = _.GetLastDisturbed();
            NWPlaceable device = Object.OBJECT_SELF;
            NWItem item = _.GetInventoryDisturbItem();

            // Check the item type to see if it's valid.
            if (!IsValidItemType(item))
            {
                _item.ReturnItem(player, item);
                player.SendMessage("You cannot reassemble this item.");
                return false;
            }

            // Serialize the item into a string and store it into the temporary data for this player. Destroy the physical item.
            var model = _craft.GetPlayerCraftingData(player);
            model.SerializedSalvageItem = _serialization.Serialize(item);
            item.Destroy();

            // Start the Atomic Reassembly conversation.
            _dialog.StartConversation(player, device, "AtomicReassembly");

            return true;
        }

        private bool IsValidItemType(NWItem item)
        {
            int type = item.BaseItemType;
            int[] validTypes =
            {
                BASE_ITEM_SHORTSWORD,
                BASE_ITEM_LONGSWORD,
                BASE_ITEM_BATTLEAXE,
                BASE_ITEM_BASTARDSWORD,
                BASE_ITEM_LIGHTFLAIL,
                BASE_ITEM_WARHAMMER,
                BASE_ITEM_HEAVYCROSSBOW,
                BASE_ITEM_LIGHTCROSSBOW,
                BASE_ITEM_LONGBOW,
                BASE_ITEM_LIGHTMACE,
                BASE_ITEM_HALBERD,
                BASE_ITEM_SHORTBOW,
                BASE_ITEM_TWOBLADEDSWORD,
                BASE_ITEM_GREATSWORD,
                BASE_ITEM_SMALLSHIELD,
                BASE_ITEM_ARMOR,
                BASE_ITEM_HELMET,
                BASE_ITEM_GREATAXE,
                BASE_ITEM_AMULET,
                BASE_ITEM_BELT,
                BASE_ITEM_DAGGER,
                BASE_ITEM_BOOTS,
                BASE_ITEM_CLUB,
                BASE_ITEM_DIREMACE,
                BASE_ITEM_DOUBLEAXE,
                BASE_ITEM_HEAVYFLAIL,
                BASE_ITEM_GLOVES,
                BASE_ITEM_LIGHTHAMMER,
                BASE_ITEM_HANDAXE,
                BASE_ITEM_KAMA,
                BASE_ITEM_KATANA,
                BASE_ITEM_KUKRI,
                BASE_ITEM_MORNINGSTAR,
                BASE_ITEM_QUARTERSTAFF,
                BASE_ITEM_RAPIER,
                BASE_ITEM_RING,
                BASE_ITEM_SCIMITAR,
                BASE_ITEM_SCYTHE,
                BASE_ITEM_LARGESHIELD,
                BASE_ITEM_TOWERSHIELD,
                BASE_ITEM_SHORTSPEAR,
                BASE_ITEM_SICKLE,
                BASE_ITEM_SLING,
                BASE_ITEM_THROWINGAXE,
                BASE_ITEM_BRACER,
                BASE_ITEM_CLOAK,
                BASE_ITEM_TRIDENT,
                BASE_ITEM_DWARVENWARAXE,
                BASE_ITEM_WHIP,
                CustomBaseItemType.Lightsaber,
                CustomBaseItemType.Saberstaff
            };

            return validTypes.Contains(type);
        }
        
    }
}
