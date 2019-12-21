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
                _.BaseItemType.Dart,
                _.BaseItemType.ThrowingAxe,
                _.BaseItemType.Shuriken,
                _.BaseItemType.ShortSword,
                _.BaseItemType.LongSword,
                _.BaseItemType.BattleAxe,
                _.BaseItemType.BastardSword,
                _.BaseItemType.LightFlail,
                _.BaseItemType.Warhammer,
                _.BaseItemType.HeavyCrossBow,
                _.BaseItemType.LightCrossBow,
                _.BaseItemType.LongBow,
                _.BaseItemType.LightMace,
                _.BaseItemType.Halberd,
                _.BaseItemType.ShortBow,
                _.BaseItemType.TwoBladedSword,
                _.BaseItemType.GreatSword,
                _.BaseItemType.SmallShield,
                _.BaseItemType.Armor,
                _.BaseItemType.Helmet,
                _.BaseItemType.GreatAxe,
                _.BaseItemType.Amulet,
                _.BaseItemType.Belt,
                _.BaseItemType.Dagger,
                _.BaseItemType.Boots,
                _.BaseItemType.Club,
                _.BaseItemType.DireMace,
                _.BaseItemType.DoubleAxe,
                _.BaseItemType.HeavyFlail,
                _.BaseItemType.Gloves,
                _.BaseItemType.LightHammer,
                _.BaseItemType.HandAxe,
                _.BaseItemType.Kama,
                _.BaseItemType.Katana,
                _.BaseItemType.Kukri,
                _.BaseItemType.Morningstar,
                _.BaseItemType.QuarterStaff,
                _.BaseItemType.Rapier,
                _.BaseItemType.Ring,
                _.BaseItemType.Scimitar,
                _.BaseItemType.Scythe,
                _.BaseItemType.LargeShield,
                _.BaseItemType.TowerShield,
                _.BaseItemType.ShortSpear,
                _.BaseItemType.Sickle,
                _.BaseItemType.Sling,
                _.BaseItemType.ThrowingAxe,
                _.BaseItemType.Bracer,
                _.BaseItemType.Cloak,
                _.BaseItemType.Trident,
                _.BaseItemType.DwarvenWaraxe,
                _.BaseItemType.Whip,
                BaseItemType.Lightsaber,
                BaseItemType.Saberstaff
            };

            return validTypes.Contains(type);
        }
        
    }
}
