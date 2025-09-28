using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Bioware;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Component.Inventory.Feature
{
    public class StandardItemConfigurations
    {
        /// <summary>
        /// These are valid item types which will receive the OnHitCastSpell item property.
        /// Anything outside this set will not have this item property added automatically.
        /// </summary>
        private static readonly BaseItemType[] _validItemTypes = {
                    BaseItemType.Armor,
                    BaseItemType.Arrow,
                    BaseItemType.BastardSword,
                    BaseItemType.BattleAxe,
                    BaseItemType.Belt,
                    BaseItemType.Bolt,
                    BaseItemType.Boots,
                    BaseItemType.Bracer,
                    BaseItemType.Bullet,
                    BaseItemType.Cloak,
                    BaseItemType.Club,
                    BaseItemType.Dagger,
                    BaseItemType.Dart,
                    BaseItemType.DireMace,
                    BaseItemType.DoubleAxe,
                    BaseItemType.DwarvenWarAxe,
                    BaseItemType.Gloves,
                    BaseItemType.GreatAxe,
                    BaseItemType.GreatSword,
                    BaseItemType.Grenade,
                    BaseItemType.Halberd,
                    BaseItemType.HandAxe,
                    BaseItemType.Cannon,
                    BaseItemType.HeavyFlail,
                    BaseItemType.Helmet,
                    BaseItemType.Kama,
                    BaseItemType.Katana,
                    BaseItemType.Kukri,
                    BaseItemType.LargeShield,
                    BaseItemType.Rifle,
                    BaseItemType.LightFlail,
                    BaseItemType.LightHammer,
                    BaseItemType.LightMace,
                    BaseItemType.Longbow,
                    BaseItemType.Longsword,
                    BaseItemType.MorningStar,
                    BaseItemType.QuarterStaff,
                    BaseItemType.Rapier,
                    BaseItemType.Scimitar,
                    BaseItemType.Scythe,
                    BaseItemType.Pistol,
                    BaseItemType.ShortSpear,
                    BaseItemType.ShortSword,
                    BaseItemType.Shuriken,
                    BaseItemType.Sickle,
                    BaseItemType.Sling,
                    BaseItemType.SmallShield,
                    BaseItemType.ThrowingAxe,
                    BaseItemType.TowerShield,
                    BaseItemType.Trident,
                    BaseItemType.TwoBladedSword,
                    BaseItemType.WarHammer,
                    BaseItemType.Whip,
                    BaseItemType.Katar,
                    BaseItemType.Lightsaber,
                    BaseItemType.Saberstaff,
                    BaseItemType.Electroblade,
                    BaseItemType.TwinElectroBlade
        };

        /// <summary>
        /// Whenever an item with an approved base item type is equipped, the OnHitCastSpell item property will be added to it.
        /// Arrows, bolts, and bullets will also receive this item property if they're equipped.
        /// </summary>
        [ScriptHandler<OnModuleEquip>]
        public void AddOnHitProperty()
        {
            var player = GetPCItemLastEquippedBy();
            if (!GetIsPC(player) || GetIsDM(player)) return;
            var item = GetPCItemLastEquipped();

            var baseItemType = GetBaseItemType(item);
            if (!_validItemTypes.Contains(baseItemType)) return;

            var arrows = GetItemInSlot(InventorySlotType.Arrows, player);
            var bolts = GetItemInSlot(InventorySlotType.Bolts, player);
            var bullets = GetItemInSlot(InventorySlotType.Bullets, player);

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
                    if (GetItemPropertySubType(ip) == (int)ItemPropertyOnHitCastSpellType.ONHIT_UNIQUEPOWER)
                    {
                        return;
                    }
                }
            }

            // No item property found. Add it to the item.
            BiowareXP2.IPSafeAddItemProperty(item, ItemPropertyOnHitCastSpell(ItemPropertyOnHitCastSpellType.ONHIT_UNIQUEPOWER, 40), 0.0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
        }
    }
}
