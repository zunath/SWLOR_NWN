using System.Linq;
using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;
using _ = SWLOR.Game.Server.NWScript._;

namespace SWLOR.Game.Server.Scripting.Placeable.MolecularReassembler
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
            if (_.GetInventoryDisturbType() != InventoryDisturbType.Added)
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
            var type = item.BaseItemType;
            BaseItemType[] validTypes =
            {
                BaseItemType.Dart,
                BaseItemType.ThrowingAxe,
                BaseItemType.Shuriken,
                BaseItemType.ShortSword,
                BaseItemType.LongSword,
                BaseItemType.BattleAxe,
                BaseItemType.BastardSword,
                BaseItemType.LightFlail,
                BaseItemType.Warhammer,
                BaseItemType.HeavyCrossBow,
                BaseItemType.LightCrossBow,
                BaseItemType.LongBow,
                BaseItemType.LightMace,
                BaseItemType.Halberd,
                BaseItemType.ShortBow,
                BaseItemType.TwoBladedSword,
                BaseItemType.GreatSword,
                BaseItemType.SmallShield,
                BaseItemType.Armor,
                BaseItemType.Helmet,
                BaseItemType.GreatAxe,
                BaseItemType.Amulet,
                BaseItemType.Belt,
                BaseItemType.Dagger,
                BaseItemType.Boots,
                BaseItemType.Club,
                BaseItemType.DireMace,
                BaseItemType.DoubleAxe,
                BaseItemType.HeavyFlail,
                BaseItemType.Gloves,
                BaseItemType.LightHammer,
                BaseItemType.HandAxe,
                BaseItemType.Kama,
                BaseItemType.Katana,
                BaseItemType.Kukri,
                BaseItemType.Morningstar,
                BaseItemType.QuarterStaff,
                BaseItemType.Rapier,
                BaseItemType.Ring,
                BaseItemType.Scimitar,
                BaseItemType.Scythe,
                BaseItemType.LargeShield,
                BaseItemType.TowerShield,
                BaseItemType.ShortSpear,
                BaseItemType.Sickle,
                BaseItemType.Sling,
                BaseItemType.ThrowingAxe,
                BaseItemType.Bracer,
                BaseItemType.Cloak,
                BaseItemType.Trident,
                BaseItemType.DwarvenWaraxe,
                BaseItemType.Whip,
                BaseItemType.Lightsaber,
                BaseItemType.Saberstaff
            };

            return validTypes.Contains(type);
        }
        
    }
}
