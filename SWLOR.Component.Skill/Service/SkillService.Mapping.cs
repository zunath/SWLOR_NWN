using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Skill.Service
{
    public partial class SkillService
    {
        private readonly Dictionary<BaseItemType, SkillType> _itemToSkillMapping = new();

        /// <summary>
        /// Handles creating all of the mapping dictionaries used by the skill system on module load.
        /// </summary>
        public void LoadMappings()
        {
            LoadItemToSkillMapping();
        }

        /// <summary>
        /// Loads the base item -> skill type mappings.
        /// </summary>
        private void LoadItemToSkillMapping()
        {
            // One-Handed Skills
            _itemToSkillMapping[BaseItemType.BastardSword] = SkillType.OneHanded;
            _itemToSkillMapping[BaseItemType.BattleAxe] = SkillType.OneHanded;
            _itemToSkillMapping[BaseItemType.Dagger] = SkillType.OneHanded;
            _itemToSkillMapping[BaseItemType.HandAxe] = SkillType.OneHanded;
            _itemToSkillMapping[BaseItemType.Kama] = SkillType.OneHanded;
            _itemToSkillMapping[BaseItemType.Katana] = SkillType.OneHanded;
            _itemToSkillMapping[BaseItemType.Kukri] = SkillType.OneHanded;
            _itemToSkillMapping[BaseItemType.LightFlail] = SkillType.OneHanded;
            _itemToSkillMapping[BaseItemType.LightHammer] = SkillType.OneHanded;
            _itemToSkillMapping[BaseItemType.LightMace] = SkillType.OneHanded;
            _itemToSkillMapping[BaseItemType.Longsword] = SkillType.OneHanded;
            _itemToSkillMapping[BaseItemType.MorningStar] = SkillType.OneHanded;
            _itemToSkillMapping[BaseItemType.Rapier] = SkillType.OneHanded;
            _itemToSkillMapping[BaseItemType.Scimitar] = SkillType.OneHanded;
            _itemToSkillMapping[BaseItemType.ShortSword] = SkillType.OneHanded;
            _itemToSkillMapping[BaseItemType.Sickle] = SkillType.OneHanded;
            _itemToSkillMapping[BaseItemType.Whip] = SkillType.OneHanded;
            _itemToSkillMapping[BaseItemType.Lightsaber] = SkillType.OneHanded;
            _itemToSkillMapping[BaseItemType.Electroblade] = SkillType.OneHanded;

            // Two-Handed Skills
            _itemToSkillMapping[BaseItemType.DireMace] = SkillType.TwoHanded;
            _itemToSkillMapping[BaseItemType.DwarvenWarAxe] = SkillType.TwoHanded;
            _itemToSkillMapping[BaseItemType.GreatAxe] = SkillType.TwoHanded;
            _itemToSkillMapping[BaseItemType.GreatSword] = SkillType.TwoHanded;
            _itemToSkillMapping[BaseItemType.Halberd] = SkillType.TwoHanded;
            _itemToSkillMapping[BaseItemType.HeavyFlail] = SkillType.TwoHanded;
            _itemToSkillMapping[BaseItemType.Scythe] = SkillType.TwoHanded;
            _itemToSkillMapping[BaseItemType.Trident] = SkillType.TwoHanded;
            _itemToSkillMapping[BaseItemType.WarHammer] = SkillType.TwoHanded;
            _itemToSkillMapping[BaseItemType.ShortSpear] = SkillType.TwoHanded;
            _itemToSkillMapping[BaseItemType.TwoBladedSword] = SkillType.TwoHanded;
            _itemToSkillMapping[BaseItemType.DoubleAxe] = SkillType.TwoHanded;
            _itemToSkillMapping[BaseItemType.Saberstaff] = SkillType.TwoHanded;
            _itemToSkillMapping[BaseItemType.TwinElectroBlade] = SkillType.TwoHanded;

            // Martial Arts Skills
            _itemToSkillMapping[BaseItemType.Club] = SkillType.MartialArts;
            _itemToSkillMapping[BaseItemType.Bracer] = SkillType.MartialArts;
            _itemToSkillMapping[BaseItemType.Gloves] = SkillType.MartialArts;
            _itemToSkillMapping[BaseItemType.QuarterStaff] = SkillType.MartialArts;
            _itemToSkillMapping[BaseItemType.Katar] = SkillType.MartialArts;

            // Ranged Skills
            _itemToSkillMapping[BaseItemType.Cannon] = SkillType.Ranged;
            _itemToSkillMapping[BaseItemType.Rifle] = SkillType.Ranged;
            _itemToSkillMapping[BaseItemType.Longbow] = SkillType.Ranged;
            _itemToSkillMapping[BaseItemType.Pistol] = SkillType.Ranged;
            _itemToSkillMapping[BaseItemType.Arrow] = SkillType.Ranged;
            _itemToSkillMapping[BaseItemType.Bolt] = SkillType.Ranged;
            _itemToSkillMapping[BaseItemType.Bullet] = SkillType.Ranged;
            _itemToSkillMapping[BaseItemType.Sling] = SkillType.Ranged;
            _itemToSkillMapping[BaseItemType.Grenade] = SkillType.Ranged;
            _itemToSkillMapping[BaseItemType.Shuriken] = SkillType.Ranged;
            _itemToSkillMapping[BaseItemType.ThrowingAxe] = SkillType.Ranged;
            _itemToSkillMapping[BaseItemType.Dart] = SkillType.Ranged;

            Console.WriteLine($"Loaded {_itemToSkillMapping.Count} item to skill mappings.");
        }

        /// <summary>
        /// Retrieves the skill type associated with a base item type.
        /// If no skill is associated with the item, SkillType.Invalid will be returned.
        /// </summary>
        /// <param name="baseItem">The type of base item to look for.</param>
        /// <returns>A skill type associated with the given base item type.</returns>
        public SkillType GetSkillTypeByBaseItem(BaseItemType baseItem)
        {
            if (!_itemToSkillMapping.ContainsKey(baseItem))
                return SkillType.Invalid;

            return _itemToSkillMapping[baseItem];
        }
    }
}
