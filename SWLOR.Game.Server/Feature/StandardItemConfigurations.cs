using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Core.NWScript.Enum.Item.Property;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature
{
    public static class StandardItemConfigurations
    {
        /// <summary>
        /// When a player connects to the server, a fist glove will be spawned and equipped
        /// if the player does not have anything in the gloves slot already.
        /// </summary>
        [NWNEventHandler("mod_enter")]
        public static void EquipFistGloveOnEntry()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            ForceEquipFistGlove(player);
        }

        /// <summary>
        /// These are valid item types which will receive the OnHitCastSpell item property.
        /// Anything outside this set will not have this item property added automatically.
        /// </summary>
        private static BaseItem[] _validItemTypes = {
                    BaseItem.Armor,
                    BaseItem.Arrow,
                    BaseItem.BastardSword,
                    BaseItem.BattleAxe,
                    BaseItem.Belt,
                    BaseItem.Bolt,
                    BaseItem.Boots,
                    BaseItem.Bracer,
                    BaseItem.Bullet,
                    BaseItem.Cloak,
                    BaseItem.Club,
                    BaseItem.Dagger,
                    BaseItem.Dart,
                    BaseItem.DireMace,
                    BaseItem.DoubleAxe,
                    BaseItem.DwarvenWarAxe,
                    BaseItem.Gloves,
                    BaseItem.GreatAxe,
                    BaseItem.GreatSword,
                    BaseItem.Grenade,
                    BaseItem.Halberd,
                    BaseItem.HandAxe,
                    BaseItem.Cannon,
                    BaseItem.HeavyFlail,
                    BaseItem.Helmet,
                    BaseItem.Kama,
                    BaseItem.Katana,
                    BaseItem.Kukri,
                    BaseItem.LargeShield,
                    BaseItem.Rifle,
                    BaseItem.LightFlail,
                    BaseItem.LightHammer,
                    BaseItem.LightMace,
                    BaseItem.Longbow,
                    BaseItem.Longsword,
                    BaseItem.MorningStar,
                    BaseItem.QuarterStaff,
                    BaseItem.Rapier,
                    BaseItem.Scimitar,
                    BaseItem.Scythe,
                    BaseItem.Pistol,
                    BaseItem.ShortSpear,
                    BaseItem.ShortSword,
                    BaseItem.Shuriken,
                    BaseItem.Sickle,
                    BaseItem.Sling,
                    BaseItem.SmallShield,
                    BaseItem.ThrowingAxe,
                    BaseItem.TowerShield,
                    BaseItem.Trident,
                    BaseItem.TwoBladedSword,
                    BaseItem.WarHammer,
                    BaseItem.Whip
        };

        /// <summary>
        /// Whenever an item with an approved base item type is equipped, the OnHitCastSpell item property will be added to it.
        /// Arrows, bolts, and bullets will also receive this item property if they're equipped.
        /// </summary>
        [NWNEventHandler("mod_equip")]
        public static void AddOnHitProperty()
        {
            var player = GetPCItemLastEquippedBy();
            if (!GetIsPC(player) || GetIsDM(player)) return;
            var item = GetPCItemLastEquipped();

            var baseItemType = GetBaseItemType(item);
            if (!_validItemTypes.Contains(baseItemType)) return;

            var arrows = GetItemInSlot(InventorySlot.Arrows, player);
            var bolts = GetItemInSlot(InventorySlot.Bolts, player);
            var bullets = GetItemInSlot(InventorySlot.Bullets, player);

            ApplyOnHitProperty(item);
            ApplyOnHitProperty(arrows);
            ApplyOnHitProperty(bolts);
            ApplyOnHitProperty(bullets);
        }

        /// <summary>
        /// Applies the OnHitCastSpell item property to a specified item.
        /// </summary>
        /// <param name="item"></param>
        private static void ApplyOnHitProperty(uint item)
        {
            for(var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(ip) == ItemPropertyType.OnHitCastSpell)
                {
                    if (GetItemPropertySubType(ip) == (int)OnHitCastSpell.ONHIT_UNIQUEPOWER)
                    {
                        return;
                    }
                }
            }

            // No item property found. Add it to the item.
            BiowareXP2.IPSafeAddItemProperty(item, ItemPropertyOnHitCastSpell(OnHitCastSpellType.ONHIT_UNIQUEPOWER, 40), 0.0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
        }

        /// <summary>
        /// When a fist item is unequipped, it is destroyed.
        /// A new one will be created and equipped by the player.
        /// </summary>
        [NWNEventHandler("mod_unequip")]
        public static void EquipFistGloveOnUnequip()
        {
            var player = GetPCItemLastUnequippedBy();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var item = GetPCItemLastUnequipped();
            var type = GetBaseItemType(item);

            if (type != BaseItem.Bracer && type != BaseItem.Gloves) return;
            var resref = GetResRef(item);

            // If fist was unequipped, destroy it.
            if (resref == "fist")
            {
                DestroyObject(item);
            }

            // Remove any other fists in the PC's inventory.
            var inventory = GetFirstItemInInventory(player);
            while(GetIsObjectValid(inventory))
            {
                if (GetResRef(inventory) == "fist")
                {
                    DestroyObject(inventory);
                }

                inventory = GetNextItemInInventory(player);
            }

            // Check in 1 second to see if PC has a glove equipped. If they don't, create a fist glove and equip it.
            ForceEquipFistGlove(player);
        }

        /// <summary>
        /// Checks a player's gloves slot. If it's empty, a fist item will be created and the player will equip it.
        /// </summary>
        /// <param name="player">The player who will equip the fist item.</param>
        private static void ForceEquipFistGlove(uint player)
        {
            DelayCommand(1.0f, () =>
            {
                var gloves = GetItemInSlot(InventorySlot.Arms, player);
                if (!GetIsObjectValid(gloves))
                {
                    AssignCommand(player, () => ClearAllActions());
                    var glove = CreateItemOnObject("fist", player);
                    AssignCommand(player, () => ActionEquipItem(glove, InventorySlot.Arms));
                }
            });
        }
    }
}
