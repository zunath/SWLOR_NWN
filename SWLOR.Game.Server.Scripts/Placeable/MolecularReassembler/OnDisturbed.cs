using System.Linq;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripts.Placeable.MolecularReassembler
{
    public class OnDisturbed: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            if (_.GetInventoryDisturbType() != _.INVENTORY_DISTURB_TYPE_ADDED)
                return;
            
            NWPlayer player = _.GetLastDisturbed();
            NWPlaceable device = NWGameObject.OBJECT_SELF;
            NWItem item = _.GetInventoryDisturbItem();

            // Check the item type to see if it's valid.
            if (!IsValidItemType(item))
            {
                ItemService.ReturnItem(player, item);
                player.SendMessage("You cannot reassemble this item.");
                return;
            }

            // Only crafted items can be reassembled.
            if (string.IsNullOrWhiteSpace(item.GetLocalString("CRAFTER_PLAYER_ID")))
            {
                ItemService.ReturnItem(player, item);
                player.SendMessage("Only crafted items may be reassembled.");
                return;
            }

            // DMs cannot reassemble because they don't have the necessary DB records.
            if (player.IsDM)
            {
                ItemService.ReturnItem(player, item);
                player.SendMessage("DMs cannot reassemble items at this time.");
                return;
            }

            // Serialize the item into a string and store it into the temporary data for this player. Destroy the physical item.
            var model = CraftService.GetPlayerCraftingData(player);
            model.SerializedSalvageItem = SerializationService.Serialize(item);
            item.Destroy();

            // Start the Molecular Reassembly conversation.
            DialogService.StartConversation(player, device, "MolecularReassembly");
        }

        private bool IsValidItemType(NWItem item)
        {
            int type = item.BaseItemType;
            int[] validTypes =
            {
                _.BASE_ITEM_SHORTSWORD,
                _.BASE_ITEM_LONGSWORD,
                _.BASE_ITEM_BATTLEAXE,
                _.BASE_ITEM_BASTARDSWORD,
                _.BASE_ITEM_LIGHTFLAIL,
                _.BASE_ITEM_WARHAMMER,
                _.BASE_ITEM_HEAVYCROSSBOW,
                _.BASE_ITEM_LIGHTCROSSBOW,
                _.BASE_ITEM_LONGBOW,
                _.BASE_ITEM_LIGHTMACE,
                _.BASE_ITEM_HALBERD,
                _.BASE_ITEM_SHORTBOW,
                _.BASE_ITEM_TWOBLADEDSWORD,
                _.BASE_ITEM_GREATSWORD,
                _.BASE_ITEM_SMALLSHIELD,
                _.BASE_ITEM_ARMOR,
                _.BASE_ITEM_HELMET,
                _.BASE_ITEM_GREATAXE,
                _.BASE_ITEM_AMULET,
                _.BASE_ITEM_BELT,
                _.BASE_ITEM_DAGGER,
                _.BASE_ITEM_BOOTS,
                _.BASE_ITEM_CLUB,
                _.BASE_ITEM_DIREMACE,
                _.BASE_ITEM_DOUBLEAXE,
                _.BASE_ITEM_HEAVYFLAIL,
                _.BASE_ITEM_GLOVES,
                _.BASE_ITEM_LIGHTHAMMER,
                _.BASE_ITEM_HANDAXE,
                _.BASE_ITEM_KAMA,
                _.BASE_ITEM_KATANA,
                _.BASE_ITEM_KUKRI,
                _.BASE_ITEM_MORNINGSTAR,
                _.BASE_ITEM_QUARTERSTAFF,
                _.BASE_ITEM_RAPIER,
                _.BASE_ITEM_RING,
                _.BASE_ITEM_SCIMITAR,
                _.BASE_ITEM_SCYTHE,
                _.BASE_ITEM_LARGESHIELD,
                _.BASE_ITEM_TOWERSHIELD,
                _.BASE_ITEM_SHORTSPEAR,
                _.BASE_ITEM_SICKLE,
                _.BASE_ITEM_SLING,
                _.BASE_ITEM_THROWINGAXE,
                _.BASE_ITEM_BRACER,
                _.BASE_ITEM_CLOAK,
                _.BASE_ITEM_TRIDENT,
                _.BASE_ITEM_DWARVENWARAXE,
                _.BASE_ITEM_WHIP,
                CustomBaseItemType.Lightsaber,
                CustomBaseItemType.Saberstaff
            };

            return validTypes.Contains(type);
        }
        
    }
}
