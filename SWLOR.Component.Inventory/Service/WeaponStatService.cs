using NWN.Native.API;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Inventory.ValueObjects;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Inventory.Service
{
    internal class WeaponStatService: IWeaponStatService
    {
        private readonly Dictionary<BaseItemType, AbilityType> _itemToDamageAbilityMapping = new()
        {
            // One-Handed Skills
            { BaseItemType.BastardSword, AbilityType.Might },
            { BaseItemType.BattleAxe, AbilityType.Might },
            { BaseItemType.Dagger, AbilityType.Perception },
            { BaseItemType.HandAxe, AbilityType.Might },
            { BaseItemType.Kama, AbilityType.Perception },
            { BaseItemType.Katana, AbilityType.Might },
            { BaseItemType.Kukri, AbilityType.Perception },
            { BaseItemType.LightFlail, AbilityType.Might },
            { BaseItemType.LightHammer, AbilityType.Might },
            { BaseItemType.LightMace, AbilityType.Might },
            { BaseItemType.Longsword, AbilityType.Might },
            { BaseItemType.MorningStar, AbilityType.Might },
            { BaseItemType.Rapier, AbilityType.Perception },
            { BaseItemType.Scimitar, AbilityType.Might },
            { BaseItemType.ShortSword, AbilityType.Perception },
            { BaseItemType.Sickle, AbilityType.Perception },
            { BaseItemType.Whip, AbilityType.Perception },
            { BaseItemType.Lightsaber, AbilityType.Perception },
            { BaseItemType.Electroblade, AbilityType.Perception },

            // Two-Handed Skills
            { BaseItemType.DireMace, AbilityType.Might },
            { BaseItemType.DwarvenWarAxe, AbilityType.Might },
            { BaseItemType.GreatAxe, AbilityType.Might },
            { BaseItemType.GreatSword, AbilityType.Might },
            { BaseItemType.Halberd, AbilityType.Might },
            { BaseItemType.HeavyFlail, AbilityType.Might },
            { BaseItemType.Scythe, AbilityType.Might },
            { BaseItemType.Trident, AbilityType.Might },
            { BaseItemType.WarHammer, AbilityType.Might },
            { BaseItemType.ShortSpear, AbilityType.Might },
            { BaseItemType.TwoBladedSword, AbilityType.Might },
            { BaseItemType.DoubleAxe, AbilityType.Might },
            { BaseItemType.Saberstaff, AbilityType.Perception },
            { BaseItemType.TwinElectroBlade, AbilityType.Perception },

            // Martial Arts Skills
            { BaseItemType.Club, AbilityType.Might },
            { BaseItemType.Bracer, AbilityType.Might },
            { BaseItemType.Gloves, AbilityType.Might },
            { BaseItemType.QuarterStaff, AbilityType.Might },
            { BaseItemType.Katar, AbilityType.Perception },

            // Ranged Skills
            { BaseItemType.Cannon, AbilityType.Perception },
            { BaseItemType.Rifle, AbilityType.Perception },
            { BaseItemType.Longbow, AbilityType.Perception },
            { BaseItemType.Pistol, AbilityType.Perception },
            { BaseItemType.Arrow, AbilityType.Perception },
            { BaseItemType.Bolt, AbilityType.Perception },
            { BaseItemType.Bullet, AbilityType.Perception },
            { BaseItemType.Sling, AbilityType.Perception },
            { BaseItemType.Grenade, AbilityType.Perception },
            { BaseItemType.Shuriken, AbilityType.Might },
            { BaseItemType.ThrowingAxe, AbilityType.Might },
            { BaseItemType.Dart, AbilityType.Might },

            // NPCs
            { BaseItemType.CreatureBludgeonWeapon, AbilityType.Might },
            { BaseItemType.CreaturePierceWeapon, AbilityType.Perception },
            { BaseItemType.CreatureSlashPierceWeapon, AbilityType.Might },
            { BaseItemType.CreatureSlashWeapon, AbilityType.Might },
        };

        private readonly Dictionary<BaseItemType, AbilityType> _itemToAccuracyAbilityMapping = new()
        {
            // One-Handed Skills
            { BaseItemType.BastardSword, AbilityType.Perception },
            { BaseItemType.BattleAxe, AbilityType.Perception },
            { BaseItemType.Dagger, AbilityType.Agility },
            { BaseItemType.HandAxe, AbilityType.Perception },
            { BaseItemType.Kama, AbilityType.Agility },
            { BaseItemType.Katana, AbilityType.Perception },
            { BaseItemType.Kukri, AbilityType.Agility },
            { BaseItemType.LightFlail, AbilityType.Perception },
            { BaseItemType.LightHammer, AbilityType.Perception },
            { BaseItemType.LightMace, AbilityType.Perception },
            { BaseItemType.Longsword, AbilityType.Perception },
            { BaseItemType.MorningStar, AbilityType.Perception },
            { BaseItemType.Rapier, AbilityType.Agility },
            { BaseItemType.Scimitar, AbilityType.Perception },
            { BaseItemType.ShortSword, AbilityType.Agility },
            { BaseItemType.Sickle, AbilityType.Agility },
            { BaseItemType.Whip, AbilityType.Agility },
            { BaseItemType.Lightsaber, AbilityType.Agility },
            { BaseItemType.Electroblade, AbilityType.Agility },

            // Two-Handed Skills
            { BaseItemType.DireMace, AbilityType.Perception },
            { BaseItemType.DwarvenWarAxe, AbilityType.Perception },
            { BaseItemType.GreatAxe, AbilityType.Perception },
            { BaseItemType.GreatSword, AbilityType.Perception },
            { BaseItemType.Halberd, AbilityType.Perception },
            { BaseItemType.HeavyFlail, AbilityType.Perception },
            { BaseItemType.Scythe, AbilityType.Perception },
            { BaseItemType.Trident, AbilityType.Perception },
            { BaseItemType.WarHammer, AbilityType.Perception },
            { BaseItemType.ShortSpear, AbilityType.Perception },
            { BaseItemType.TwoBladedSword, AbilityType.Agility },
            { BaseItemType.DoubleAxe, AbilityType.Agility },
            { BaseItemType.Saberstaff, AbilityType.Agility },
            { BaseItemType.TwinElectroBlade, AbilityType.Agility },

            // Martial Arts Skills
            { BaseItemType.Club, AbilityType.Perception },
            { BaseItemType.Bracer, AbilityType.Perception },
            { BaseItemType.Gloves, AbilityType.Perception },
            { BaseItemType.QuarterStaff, AbilityType.Perception },
            { BaseItemType.Katar, AbilityType.Agility },

            // Ranged Skills
            { BaseItemType.Cannon, AbilityType.Agility },
            { BaseItemType.Rifle, AbilityType.Agility },
            { BaseItemType.Longbow, AbilityType.Agility },
            { BaseItemType.Pistol, AbilityType.Agility },
            { BaseItemType.Arrow, AbilityType.Agility },
            { BaseItemType.Bolt, AbilityType.Agility },
            { BaseItemType.Bullet, AbilityType.Agility },
            { BaseItemType.Sling, AbilityType.Agility },
            { BaseItemType.Grenade, AbilityType.Agility },
            { BaseItemType.Shuriken, AbilityType.Agility },
            { BaseItemType.ThrowingAxe, AbilityType.Agility },
            { BaseItemType.Dart, AbilityType.Agility },

            // NPCs
            { BaseItemType.CreatureBludgeonWeapon, AbilityType.Perception },
            { BaseItemType.CreaturePierceWeapon, AbilityType.Perception },
            { BaseItemType.CreatureSlashPierceWeapon, AbilityType.Perception },
            { BaseItemType.CreatureSlashWeapon, AbilityType.Perception },
        };

        private readonly Dictionary<BaseItemType, SkillType> _itemToSkillMapping = new()
        {
            // One-Handed Skills
            { BaseItemType.BastardSword, SkillType.OneHanded },
            { BaseItemType.BattleAxe, SkillType.OneHanded },
            { BaseItemType.Dagger, SkillType.OneHanded },
            { BaseItemType.HandAxe, SkillType.OneHanded },
            { BaseItemType.Kama, SkillType.OneHanded },
            { BaseItemType.Katana, SkillType.OneHanded },
            { BaseItemType.Kukri, SkillType.OneHanded },
            { BaseItemType.LightFlail, SkillType.OneHanded },
            { BaseItemType.LightHammer, SkillType.OneHanded },
            { BaseItemType.LightMace, SkillType.OneHanded },
            { BaseItemType.Longsword, SkillType.OneHanded },
            { BaseItemType.MorningStar, SkillType.OneHanded },
            { BaseItemType.Rapier, SkillType.OneHanded },
            { BaseItemType.Scimitar, SkillType.OneHanded },
            { BaseItemType.ShortSword, SkillType.OneHanded },
            { BaseItemType.Sickle, SkillType.OneHanded },
            { BaseItemType.Whip, SkillType.OneHanded },
            { BaseItemType.Lightsaber, SkillType.OneHanded },
            { BaseItemType.Electroblade, SkillType.OneHanded },

            // Two-Handed Skills
            { BaseItemType.DireMace, SkillType.TwoHanded },
            { BaseItemType.DwarvenWarAxe, SkillType.TwoHanded },
            { BaseItemType.GreatAxe, SkillType.TwoHanded },
            { BaseItemType.GreatSword, SkillType.TwoHanded },
            { BaseItemType.Halberd, SkillType.TwoHanded },
            { BaseItemType.HeavyFlail, SkillType.TwoHanded },
            { BaseItemType.Scythe, SkillType.TwoHanded },
            { BaseItemType.Trident, SkillType.TwoHanded },
            { BaseItemType.WarHammer, SkillType.TwoHanded },
            { BaseItemType.ShortSpear, SkillType.TwoHanded },
            { BaseItemType.TwoBladedSword, SkillType.TwoHanded },
            { BaseItemType.DoubleAxe, SkillType.TwoHanded },
            { BaseItemType.Saberstaff, SkillType.TwoHanded },
            { BaseItemType.TwinElectroBlade, SkillType.TwoHanded },

            // Martial Arts Skills
            { BaseItemType.Club, SkillType.MartialArts },
            { BaseItemType.Bracer, SkillType.MartialArts },
            { BaseItemType.Gloves, SkillType.MartialArts },
            { BaseItemType.QuarterStaff, SkillType.MartialArts },
            { BaseItemType.Katar, SkillType.MartialArts },

            // Ranged Skills
            { BaseItemType.Cannon, SkillType.Ranged },
            { BaseItemType.Rifle, SkillType.Ranged },
            { BaseItemType.Longbow, SkillType.Ranged },
            { BaseItemType.Pistol, SkillType.Ranged },
            { BaseItemType.Arrow, SkillType.Ranged },
            { BaseItemType.Bolt, SkillType.Ranged },
            { BaseItemType.Bullet, SkillType.Ranged },
            { BaseItemType.Sling, SkillType.Ranged },
            { BaseItemType.Grenade, SkillType.Ranged },
            { BaseItemType.Shuriken, SkillType.Ranged },
            { BaseItemType.ThrowingAxe, SkillType.Ranged },
            { BaseItemType.Dart, SkillType.Ranged }
        };


        public WeaponStat LoadWeaponStat(uint weapon)
        {
            var stat = new WeaponStat();
            if (!GetIsObjectValid(weapon))
                return stat;

            stat.Item = weapon;
            for (var ip = GetFirstItemProperty(weapon); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(weapon))
            {
                var type = GetItemPropertyType(ip);
                var value = GetItemPropertyCostTableValue(ip);
                if (type == ItemPropertyType.DMG)
                {
                    stat.DMG = value;
                }
                else if (type == ItemPropertyType.Delay)
                {
                    stat.Delay = value * 10;
                }
                else if (type == ItemPropertyType.UseLimitationPerk)
                {
                    stat.Tier = value;
                }
                else if (type == ItemPropertyType.DamageType)
                {
                    stat.DamageType = (CombatDamageType)GetItemPropertySubType(ip);
                }
            }

            stat.AccuracyStat = GetWeaponAccuracyAbilityType(weapon);
            stat.DamageStat = GetWeaponDamageAbilityType(weapon);
            stat.Skill = GetWeaponSkillType(weapon);

            return stat;
        }

        /// <summary>
        /// Retrieves the ability type tied to a particular base item type for the purposes of damage calculation.
        /// If the base item does not have an associated ability type, AbilityType.Invalid will be returned.
        /// </summary>
        /// <param name="weapon">The weapon</param>
        /// <returns>The ability type or AbilityType.Invalid if none is associated with the item.</returns>
        private AbilityType GetWeaponDamageAbilityType(uint weapon)
        {
            var itemType = GetBaseItemType(weapon);
            return !_itemToDamageAbilityMapping.ContainsKey(itemType)
                ? AbilityType.Invalid
                : _itemToDamageAbilityMapping[itemType];
        }

        /// <summary>
        /// Retrieves the ability type tied to a particular base item type for the purposes of accuracy calculation.
        /// If the base item does not have an associated ability type, AbilityType.Invalid will be returned.
        /// </summary>
        /// <param name="weapon">The weapon</param>
        /// <returns>The ability type or AbilityType.Invalid if none is associated with the item.</returns>
        private AbilityType GetWeaponAccuracyAbilityType(uint weapon)
        {
            var itemType = GetBaseItemType(weapon);
            return !_itemToAccuracyAbilityMapping.ContainsKey(itemType)
                ? AbilityType.Invalid
                : _itemToAccuracyAbilityMapping[itemType];
        }

        private SkillType GetWeaponSkillType(uint weapon)
        {
            var itemType = GetBaseItemType(weapon);
            if (!_itemToSkillMapping.ContainsKey(itemType))
                return SkillType.Invalid;

            return _itemToSkillMapping[itemType];
        }
    }
}
