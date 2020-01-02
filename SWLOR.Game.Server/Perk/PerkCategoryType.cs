using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace SWLOR.Game.Server.Perk
{
    public enum PerkCategoryType
    {
        [PerkCategory("Fabrication", true, 1)]
        Fabrication = 1,
        [PerkCategory("Lightsabers & Saberstaffs", true, 2)]
        LightsabersAndSaberstaffs = 2,
        [PerkCategory("Gathering", true, 25)]
        Gathering = 3,
        [PerkCategory("General", true, 1)]
        General = 4,
        [PerkCategory("Stances", true, 5)]
        Stances = 5,
        [PerkCategory("Shields", true, 3)]
        Shields = 6,
        [PerkCategory("Armor", true, 2)]
        Armor = 7,
        [PerkCategory("Firearms - General", true, 13)]
        FirearmsGeneral = 8,
        [PerkCategory("One-Handed - General", true, 4)]
        OneHandedGeneral = 9,
        [PerkCategory("One-Handed - Vibroblades", true, 5)]
        OneHandedVibroblades = 10,
        [PerkCategory("One-Handed - Finesse Vibroblades", true, 6)]
        OneHandedFinesseVibroblades = 11,
        [PerkCategory("One-Handed - Batons", true, 7)]
        OneHandedBatons = 12,
        [PerkCategory("Two-Handed - Heavy Vibroblades", true, 8)]
        TwoHandedHeavyVibroblades = 13,
        [PerkCategory("Saberstaffs", true, 9)]
        Saberstaffs = 14,
        [PerkCategory("Two-Handed - Polearms", true, 10)]
        TwoHandedPolearms = 15,
        [PerkCategory("Twin Blades - Twin Vibroblades", true, 11)]
        TwinBladesTwinVibroblades = 16,
        [PerkCategory("Martial Arts", true, 12)]
        MartialArts = 17,
        [PerkCategory("Blasters - Blaster Pistols", true, 14)]
        BlastersBlasterPistols = 18,
        [PerkCategory("Blasters - Blaster Rifles", true, 15)]
        BlastersBlasterRifles = 19,
        [PerkCategory("Throwing", true, 16)]
        Throwing = 20,
        [PerkCategory("Medicine", true, 22)]
        Medicine = 22,
        [PerkCategory("Weaponsmith", true, 20)]
        Weaponsmith = 23,
        [PerkCategory("Armorsmith", true, 21)]
        Armorsmith = 25,
        [PerkCategory("Cooking", false, 23)]
        Cooking = 27,
        [PerkCategory("Survival", false, 27)]
        Survival = 32,
        [PerkCategory("Engineering", true, 22)]
        Engineering = 33,
        [PerkCategory("Harvesting", true, 26)]
        Harvesting = 34,
        [PerkCategory("Lightsabers", true, 28)]
        Lightsabers = 36,
        [PerkCategory("Piloting", true, 29)]
        Piloting = 37,
        [PerkCategory("Force Alter", true, 30)]
        ForceAlter = 40,
        [PerkCategory("Force Control", true, 33)]
        ForceControl = 43,
        [PerkCategory("Force Sense", true, 36)]
        ForceSense = 46,
    }


    public static class PerkCategoryTypeExtensions
    {
        private static PerkCategoryAttribute GetAttribute(PerkCategoryType category)
        {
            var enumType = typeof(PerkCategoryType);
            var memberInfos = enumType.GetMember(category.ToString());
            var enumValueMemberInfo = memberInfos.First(m => m.DeclaringType == enumType);
            var valueAttributes = enumValueMemberInfo.GetCustomAttributes(typeof(PerkCategoryAttribute), false);
            return ((PerkCategoryAttribute)valueAttributes[0]);
        }

        public static int GetSequence(this PerkCategoryType category)
        {
            var attribute = GetAttribute(category);

            return attribute.Sequence;
        }

        public static bool GetIsActive(this PerkCategoryType category)
        {
            var attribute = GetAttribute(category);

            return attribute.IsActive;
        }

        public static string GetName(this PerkCategoryType category)
        {
            var attribute = GetAttribute(category);

            return attribute.Name;
        }
    }
}
