using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service
{
    public static partial class Skill
    {
        private static readonly Dictionary<BaseItem, SkillType> _itemToSkillMapping = new Dictionary<BaseItem, SkillType>();

        /// <summary>
        /// Handles creating all of the mapping dictionaries used by the skill system on module load.
        /// </summary>
        [NWNEventHandler("mod_load")]
        public static void LoadMappings()
        {
            LoadItemToSkillMapping();
        }

        /// <summary>
        /// Loads the base item -> skill type mappings.
        /// </summary>
        private static void LoadItemToSkillMapping()
        {
            Console.WriteLine("Loading item to skill mappings.");

            // One-Handed Skills
            _itemToSkillMapping[BaseItem.BastardSword] = SkillType.OneHanded;
            _itemToSkillMapping[BaseItem.BattleAxe] = SkillType.OneHanded;
            _itemToSkillMapping[BaseItem.Dagger] = SkillType.OneHanded;
            _itemToSkillMapping[BaseItem.HandAxe] = SkillType.OneHanded;
            _itemToSkillMapping[BaseItem.Kama] = SkillType.OneHanded;
            _itemToSkillMapping[BaseItem.Katana] = SkillType.OneHanded;
            _itemToSkillMapping[BaseItem.Kukri] = SkillType.OneHanded;
            _itemToSkillMapping[BaseItem.LightFlail] = SkillType.OneHanded;
            _itemToSkillMapping[BaseItem.LightHammer] = SkillType.OneHanded;
            _itemToSkillMapping[BaseItem.LightMace] = SkillType.OneHanded;
            _itemToSkillMapping[BaseItem.Longsword] = SkillType.OneHanded;
            _itemToSkillMapping[BaseItem.MorningStar] = SkillType.OneHanded;
            _itemToSkillMapping[BaseItem.Rapier] = SkillType.OneHanded;
            _itemToSkillMapping[BaseItem.Scimitar] = SkillType.OneHanded;
            _itemToSkillMapping[BaseItem.ShortSword] = SkillType.OneHanded;
            _itemToSkillMapping[BaseItem.Sickle] = SkillType.OneHanded;
            _itemToSkillMapping[BaseItem.Whip] = SkillType.OneHanded;

            // Two-Handed Skills
            _itemToSkillMapping[BaseItem.DireMace] = SkillType.TwoHanded;
            _itemToSkillMapping[BaseItem.DwarvenWarAxe] = SkillType.TwoHanded;
            _itemToSkillMapping[BaseItem.GreatAxe] = SkillType.TwoHanded;
            _itemToSkillMapping[BaseItem.GreatSword] = SkillType.TwoHanded;
            _itemToSkillMapping[BaseItem.Halberd] = SkillType.TwoHanded;
            _itemToSkillMapping[BaseItem.HeavyFlail] = SkillType.TwoHanded;
            _itemToSkillMapping[BaseItem.Scythe] = SkillType.TwoHanded;
            _itemToSkillMapping[BaseItem.Trident] = SkillType.TwoHanded;
            _itemToSkillMapping[BaseItem.WarHammer] = SkillType.TwoHanded;
            _itemToSkillMapping[BaseItem.ShortSpear] = SkillType.TwoHanded;
            // Twin Blades Skills
            _itemToSkillMapping[BaseItem.TwoBladedSword] = SkillType.TwinBlades;
            _itemToSkillMapping[BaseItem.DoubleAxe] = SkillType.TwinBlades;
            // Martial Arts Skills
            _itemToSkillMapping[BaseItem.Club] = SkillType.MartialArts;
            _itemToSkillMapping[BaseItem.Bracer] = SkillType.MartialArts;
            _itemToSkillMapping[BaseItem.Gloves] = SkillType.MartialArts;
            _itemToSkillMapping[BaseItem.QuarterStaff] = SkillType.MartialArts;
            // Firearms Skills
            _itemToSkillMapping[BaseItem.HeavyCrossbow] = SkillType.Blasters;
            _itemToSkillMapping[BaseItem.LightCrossbow] = SkillType.Blasters;
            _itemToSkillMapping[BaseItem.Longbow] = SkillType.Blasters;
            _itemToSkillMapping[BaseItem.ShortBow] = SkillType.Blasters;
            _itemToSkillMapping[BaseItem.Arrow] = SkillType.Blasters;
            _itemToSkillMapping[BaseItem.Bolt] = SkillType.Blasters;
            _itemToSkillMapping[BaseItem.Bullet] = SkillType.Blasters;
            _itemToSkillMapping[BaseItem.Sling] = SkillType.Blasters;
            // Throwing Skills
            _itemToSkillMapping[BaseItem.Grenade] = SkillType.Throwing;
            _itemToSkillMapping[BaseItem.Shuriken] = SkillType.Throwing;
            _itemToSkillMapping[BaseItem.ThrowingAxe] = SkillType.Throwing;
            _itemToSkillMapping[BaseItem.Dart] = SkillType.Throwing;
            // Shield Skills
            _itemToSkillMapping[BaseItem.SmallShield] = SkillType.LightArmor;
            _itemToSkillMapping[BaseItem.LargeShield] = SkillType.HeavyArmor;
            _itemToSkillMapping[BaseItem.TowerShield] = SkillType.HeavyArmor;
            // Lightsabers
            _itemToSkillMapping[BaseItem.Lightsaber] = SkillType.Lightsabers;
            _itemToSkillMapping[BaseItem.Saberstaff] = SkillType.Lightsabers;

            Console.WriteLine("Completed item to skill mappings successfully.");
        }

        /// <summary>
        /// Retrieves the skill type associated with a base item type.
        /// If no skill is associated with the item, SkillType.Invalid will be returned.
        /// </summary>
        /// <param name="baseItem">The type of base item to look for.</param>
        /// <returns>A skill type associated with the given base item type.</returns>
        public static SkillType GetSkillTypeByBaseItem(BaseItem baseItem)
        {
            if (!_itemToSkillMapping.ContainsKey(baseItem))
                return SkillType.Invalid;

            return _itemToSkillMapping[baseItem];
        }
    }
}
