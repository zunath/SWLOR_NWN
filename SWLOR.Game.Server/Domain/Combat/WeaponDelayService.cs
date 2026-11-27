using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The attack delay each weapon category carries, and the Delay item property that stores it. The logic that used to
    /// be static in <c>Service.WeaponDelay</c> is moved here unchanged. The category table is built on first use and kept
    /// in an instance field, because the constructor must not read the item service's lists. The facade keeps the event
    /// handler and forwards every call to the container's instance.
    /// </summary>
    public class WeaponDelayService : IWeaponDelayService
    {
        private readonly IItemService _item;

        /// <summary>Weapon category lists mapped to their delay values; built once, on first use.</summary>
        private Dictionary<List<BaseItem>, ItemPropertyAttackDelay> _weaponCategoryDelays;

        public WeaponDelayService(IItemService item)
        {
            _item = item;
        }

        private Dictionary<List<BaseItem>, ItemPropertyAttackDelay> WeaponCategoryDelays =>
            _weaponCategoryDelays ??= new()
            {
                {_item.VibrobladeBaseItemTypes, ItemPropertyAttackDelay.Delay230},
                {_item.KatarBaseItemTypes, ItemPropertyAttackDelay.Delay220},
                {_item.PowerGloveBaseItemTypes, ItemPropertyAttackDelay.Delay220},
                {_item.TwinBladeBaseItemTypes, ItemPropertyAttackDelay.Delay230},
                {_item.VibroknifeBaseItemTypes, ItemPropertyAttackDelay.Delay220},
                {_item.StaffBaseItemTypes, ItemPropertyAttackDelay.Delay270},
                {_item.RifleBaseItemTypes, ItemPropertyAttackDelay.Delay300},
                {_item.HeavyVibrobladeBaseItemTypes, ItemPropertyAttackDelay.Delay300},
                {_item.PistolBaseItemTypes, ItemPropertyAttackDelay.Delay250},
                {_item.LightsaberBaseItemTypes, ItemPropertyAttackDelay.Delay240},
                {_item.SpearBaseItemTypes, ItemPropertyAttackDelay.Delay280},
                {_item.ThrowingWeaponBaseItemTypes, ItemPropertyAttackDelay.Delay220},
                {_item.SaberstaffBaseItemTypes, ItemPropertyAttackDelay.Delay240},
                {_item.CreatureBaseItemTypes, ItemPropertyAttackDelay.Delay240}
            };

        public ItemPropertyAttackDelay? GetWeaponDelay(BaseItem baseItemType)
        {
            var delays = WeaponCategoryDelays;
            foreach (var category in delays.Keys)
            {
                if (category.Contains(baseItemType))
                {
                    return delays[category];
                }
            }
            return null;
        }

        public bool HasDelayProperty(uint item)
        {
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(ip) == ItemPropertyType.Delay)
                {
                    return true;
                }
            }
            return false;
        }

        public void AddDelayProperty(uint item, ItemPropertyAttackDelay delay)
        {
            if (delay == ItemPropertyAttackDelay.Invalid || !Enum.IsDefined(delay))
                throw new ArgumentOutOfRangeException(nameof(delay), delay, "Weapon attack delay must be a valid iprp_delay.2da value.");

            var delayProperty = ItemPropertyCustom(ItemPropertyType.Delay, -1, (int)delay);
            BiowareXP2.IPSafeAddItemProperty(item, delayProperty, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, true);
        }

        public bool IsWeapon(uint item)
        {
            var baseItemType = GetBaseItemType(item);
            return GetWeaponDelay(baseItemType).HasValue;
        }

        public void ApplyDelayPropertyIfMissing(uint item)
        {
            if (!IsWeapon(item))
            {
                return;
            }

            if (HasDelayProperty(item))
            {
                return;
            }

            var baseItemType = GetBaseItemType(item);
            var delay = GetWeaponDelay(baseItemType);

            if (delay.HasValue)
            {
                AddDelayProperty(item, delay.Value);
            }
        }

        [NWNEventHandler(ScriptName.OnModuleEquip)]
        public void ApplyWeaponDelayFallback()
        {
            var item = GetPCItemLastEquipped();
            if (!GetIsObjectValid(item))
                return;

            ApplyDelayPropertyIfMissing(item);
        }
    }
}
