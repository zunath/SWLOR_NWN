using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Service
{
    public static partial class Skill
    {
        private static readonly Dictionary<BaseItem, SkillType> _itemToSkillMapping = new();

        /// <summary>
        /// Handles creating all of the mapping dictionaries used by the skill system on module load.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void LoadMappings()
        {
            LoadItemToSkillMapping();
        }

        /// <summary>
        /// Loads the base item -> skill type mappings.
        /// </summary>
        private static void LoadItemToSkillMapping()
        {
            // Vibroblade
            _itemToSkillMapping[BaseItem.BastardSword] = SkillType.Vibroblade;
            _itemToSkillMapping[BaseItem.BattleAxe] = SkillType.Vibroblade;
            _itemToSkillMapping[BaseItem.Katana] = SkillType.Vibroblade;
            _itemToSkillMapping[BaseItem.Longsword] = SkillType.Vibroblade;
            _itemToSkillMapping[BaseItem.Scimitar] = SkillType.Vibroblade;

            // Vibroknife
            _itemToSkillMapping[BaseItem.Dagger] = SkillType.Vibroknife;
            _itemToSkillMapping[BaseItem.HandAxe] = SkillType.Vibroknife;
            _itemToSkillMapping[BaseItem.Kama] = SkillType.Vibroknife;
            _itemToSkillMapping[BaseItem.Kukri] = SkillType.Vibroknife;
            _itemToSkillMapping[BaseItem.Rapier] = SkillType.Vibroknife;
            _itemToSkillMapping[BaseItem.ShortSword] = SkillType.Vibroknife;
            _itemToSkillMapping[BaseItem.Sickle] = SkillType.Vibroknife;
            _itemToSkillMapping[BaseItem.Whip] = SkillType.Vibroknife;

            // Lightsaber
            _itemToSkillMapping[BaseItem.Lightsaber] = SkillType.Lightsaber;
            _itemToSkillMapping[BaseItem.Electroblade] = SkillType.Lightsaber;

            // Heavy Vibroblade
            _itemToSkillMapping[BaseItem.DireMace] = SkillType.HeavyVibroblade;
            _itemToSkillMapping[BaseItem.DwarvenWarAxe] = SkillType.HeavyVibroblade;
            _itemToSkillMapping[BaseItem.GreatAxe] = SkillType.HeavyVibroblade;
            _itemToSkillMapping[BaseItem.GreatSword] = SkillType.HeavyVibroblade;
            _itemToSkillMapping[BaseItem.HeavyFlail] = SkillType.HeavyVibroblade;
            _itemToSkillMapping[BaseItem.WarHammer] = SkillType.HeavyVibroblade;

            // Spear
            _itemToSkillMapping[BaseItem.Halberd] = SkillType.Spear;
            _itemToSkillMapping[BaseItem.Scythe] = SkillType.Spear;
            _itemToSkillMapping[BaseItem.ShortSpear] = SkillType.Spear;
            _itemToSkillMapping[BaseItem.Trident] = SkillType.Spear;

            // Twin Blade
            _itemToSkillMapping[BaseItem.TwoBladedSword] = SkillType.TwinBlade;
            _itemToSkillMapping[BaseItem.DoubleAxe] = SkillType.TwinBlade;

            // Saberstaff
            _itemToSkillMapping[BaseItem.Saberstaff] = SkillType.Saberstaff;
            _itemToSkillMapping[BaseItem.TwinElectroBlade] = SkillType.Saberstaff;

            // Katar
            _itemToSkillMapping[BaseItem.Bracer] = SkillType.Katar;
            _itemToSkillMapping[BaseItem.Gloves] = SkillType.Katar;
            _itemToSkillMapping[BaseItem.Katar] = SkillType.Katar;

            // Staff
            _itemToSkillMapping[BaseItem.Club] = SkillType.Staff;
            _itemToSkillMapping[BaseItem.LightFlail] = SkillType.Staff;
            _itemToSkillMapping[BaseItem.LightHammer] = SkillType.Staff;
            _itemToSkillMapping[BaseItem.LightMace] = SkillType.Staff;
            _itemToSkillMapping[BaseItem.MorningStar] = SkillType.Staff;
            _itemToSkillMapping[BaseItem.QuarterStaff] = SkillType.Staff;

            // Pistol
            _itemToSkillMapping[BaseItem.Pistol] = SkillType.Pistol;
            _itemToSkillMapping[BaseItem.LegacyPistol] = SkillType.Pistol;
            _itemToSkillMapping[BaseItem.Arrow] = SkillType.Pistol;
            _itemToSkillMapping[BaseItem.Bullet] = SkillType.Pistol;
            _itemToSkillMapping[BaseItem.Sling] = SkillType.Pistol;

            // Rifle
            _itemToSkillMapping[BaseItem.Cannon] = SkillType.Rifle;
            _itemToSkillMapping[BaseItem.Rifle] = SkillType.Rifle;
            _itemToSkillMapping[BaseItem.Longbow] = SkillType.Rifle;
            _itemToSkillMapping[BaseItem.Bolt] = SkillType.Rifle;
            _itemToSkillMapping[BaseItem.Grenade] = SkillType.Rifle;

            // Throwing
            _itemToSkillMapping[BaseItem.Shuriken] = SkillType.Throwing;
            _itemToSkillMapping[BaseItem.ThrowingAxe] = SkillType.Throwing;
            _itemToSkillMapping[BaseItem.Dart] = SkillType.Throwing;

            Console.WriteLine($"Loaded {_itemToSkillMapping.Count} item to skill mappings.");
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
