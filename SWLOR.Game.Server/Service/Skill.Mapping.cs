using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;

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
            _itemToSkillMapping[BaseItem.Longsword] = SkillType.Longsword;   // Knight
            _itemToSkillMapping[BaseItem.Gloves] = SkillType.Knuckles;       // Monk
            _itemToSkillMapping[BaseItem.Dagger] = SkillType.Dagger;         // Thief
            _itemToSkillMapping[BaseItem.QuarterStaff] = SkillType.Staff;    // Black Mage
            _itemToSkillMapping[BaseItem.LightMace] = SkillType.Rod;         // White Mage
            _itemToSkillMapping[BaseItem.Rapier] = SkillType.Rapier;         // Red Mage
            _itemToSkillMapping[BaseItem.Katana] = SkillType.Katana;         // Ninja
            _itemToSkillMapping[BaseItem.Gunblade] = SkillType.Gunblade;     // Specialist
            _itemToSkillMapping[BaseItem.Rifle] = SkillType.Rifle;           // Sniper
            _itemToSkillMapping[BaseItem.GreatSword] = SkillType.GreatSword; // Dark Knight
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
