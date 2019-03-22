using System.Linq;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Placeable.MolecularReassembler
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
            if (_.GetInventoryDisturbType() != NWScript.INVENTORY_DISTURB_TYPE_ADDED)
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

            // Only crafted items can be reassembled.
            if (string.IsNullOrWhiteSpace(item.GetLocalString("CRAFTER_PLAYER_ID")))
            {
                _item.ReturnItem(player, item);
                player.SendMessage("Only crafted items may be reassembled.");
                return false;
            }

            // DMs cannot reassemble because they don't have the necessary DB records.
            if (player.IsDM)
            {
                _item.ReturnItem(player, item);
                player.SendMessage("DMs cannot reassemble items at this time.");
                return false;
            }

            // Serialize the item into a string and store it into the temporary data for this player. Destroy the physical item.
            var model = _craft.GetPlayerCraftingData(player);
            model.SerializedSalvageItem = _serialization.Serialize(item);
            item.Destroy();

            // Start the Molecular Reassembly conversation.
            _dialog.StartConversation(player, device, "MolecularReassembly");

            return true;
        }

        private bool IsValidItemType(NWItem item)
        {
            int type = item.BaseItemType;
            int[] validTypes =
            {
                NWScript.BASE_ITEM_SHORTSWORD,
                NWScript.BASE_ITEM_LONGSWORD,
                NWScript.BASE_ITEM_BATTLEAXE,
                NWScript.BASE_ITEM_BASTARDSWORD,
                NWScript.BASE_ITEM_LIGHTFLAIL,
                NWScript.BASE_ITEM_WARHAMMER,
                NWScript.BASE_ITEM_HEAVYCROSSBOW,
                NWScript.BASE_ITEM_LIGHTCROSSBOW,
                NWScript.BASE_ITEM_LONGBOW,
                NWScript.BASE_ITEM_LIGHTMACE,
                NWScript.BASE_ITEM_HALBERD,
                NWScript.BASE_ITEM_SHORTBOW,
                NWScript.BASE_ITEM_TWOBLADEDSWORD,
                NWScript.BASE_ITEM_GREATSWORD,
                NWScript.BASE_ITEM_SMALLSHIELD,
                NWScript.BASE_ITEM_ARMOR,
                NWScript.BASE_ITEM_HELMET,
                NWScript.BASE_ITEM_GREATAXE,
                NWScript.BASE_ITEM_AMULET,
                NWScript.BASE_ITEM_BELT,
                NWScript.BASE_ITEM_DAGGER,
                NWScript.BASE_ITEM_BOOTS,
                NWScript.BASE_ITEM_CLUB,
                NWScript.BASE_ITEM_DIREMACE,
                NWScript.BASE_ITEM_DOUBLEAXE,
                NWScript.BASE_ITEM_HEAVYFLAIL,
                NWScript.BASE_ITEM_GLOVES,
                NWScript.BASE_ITEM_LIGHTHAMMER,
                NWScript.BASE_ITEM_HANDAXE,
                NWScript.BASE_ITEM_KAMA,
                NWScript.BASE_ITEM_KATANA,
                NWScript.BASE_ITEM_KUKRI,
                NWScript.BASE_ITEM_MORNINGSTAR,
                NWScript.BASE_ITEM_QUARTERSTAFF,
                NWScript.BASE_ITEM_RAPIER,
                NWScript.BASE_ITEM_RING,
                NWScript.BASE_ITEM_SCIMITAR,
                NWScript.BASE_ITEM_SCYTHE,
                NWScript.BASE_ITEM_LARGESHIELD,
                NWScript.BASE_ITEM_TOWERSHIELD,
                NWScript.BASE_ITEM_SHORTSPEAR,
                NWScript.BASE_ITEM_SICKLE,
                NWScript.BASE_ITEM_SLING,
                NWScript.BASE_ITEM_THROWINGAXE,
                NWScript.BASE_ITEM_BRACER,
                NWScript.BASE_ITEM_CLOAK,
                NWScript.BASE_ITEM_TRIDENT,
                NWScript.BASE_ITEM_DWARVENWARAXE,
                NWScript.BASE_ITEM_WHIP,
                CustomBaseItemType.Lightsaber,
                CustomBaseItemType.Saberstaff
            };

            return validTypes.Contains(type);
        }
        
    }
}
