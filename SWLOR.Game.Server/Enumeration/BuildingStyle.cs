using System;
using System.Collections.Generic;
using System.Text;

namespace SWLOR.Game.Server.Enumeration
{
    public enum BuildingStyle
    {
        [BuildingStyle("Invalid", "", BaseStructure.Invalid, false, "", false, BuildingType.Unknown, 0, 0, 0)]
        Invalid = 0,
        [BuildingStyle("Small Style 1 - Interior", "house_int_1", BaseStructure.SmallBuilding, true, "", true, BuildingType.Interior, 0, 0, 0)]
        SmallStyle1Interior = 1,
        [BuildingStyle("Old House - Exterior", "old_house", BaseStructure.SmallBuilding, false, "OldHouseRule", false, BuildingType.Exterior, 0, 0, 0)]
        OldHouseExterior = 2,
        [BuildingStyle("Medium Style 1 - Interior", "house_int_2", BaseStructure.MediumBuilding, true, "", true, BuildingType.Interior, 0, 0, 0)]
        MediumStyle1Interior = 3,
        [BuildingStyle("Large Style 1 - Interior", "house_int_3", BaseStructure.LargeBuilding, true, "", true, BuildingType.Interior, 0, 0, 0)]
        LargeStyle1Interior = 4,
        [BuildingStyle("Small Round Building", "house_ext_1", BaseStructure.SmallBuilding, true, "SmallRoundBuildingRule", true, BuildingType.Exterior, 0, 0, 0)]
        SmallRoundBuilding = 5,
        [BuildingStyle("Medium Round Building", "house_ext_2", BaseStructure.MediumBuilding, true, "MediumRoundBuildingRule", true, BuildingType.Exterior, 0, 0, 0)]
        MediumRoundBuilding = 6,
        [BuildingStyle("Large Square Building", "house_ext_3", BaseStructure.LargeBuilding, true, "LargeSquareBuildingRule", true, BuildingType.Exterior, 0, 0, 0)]
        LargeSquareBuilding = 7,
        [BuildingStyle("Small Apartment Style 1", "apartment_002", BaseStructure.Invalid, true, "", true, BuildingType.Apartment, 1000, 100, 30)]
        SmallApartmentStyle1 = 8,
        [BuildingStyle("Medium Apartment Style 1", "apartment_2", BaseStructure.Invalid, false, "", true, BuildingType.Apartment, 1500, 200, 40)]
        MediumApartmentStyle1 = 9,
        [BuildingStyle("Large Apartment Style 1", "apartment_3", BaseStructure.Invalid, false, "", true, BuildingType.Apartment, 2000, 300, 50)]
        LargeApartmentStyle1 = 10,
        [BuildingStyle("Small Style 2 - Interior", "house_int_4", BaseStructure.SmallBuilding, false, "", true, BuildingType.Interior, 0, 0, 0)]
        SmallStyle2Interior = 11,
        [BuildingStyle("Medium Style 2 - Interior", "house_int_5", BaseStructure.MediumBuilding, false, "", true, BuildingType.Interior, 0, 0, 0)]
        MediumStyle2Interior = 12,
        [BuildingStyle("Large Style 2 - Interior", "house_int_6", BaseStructure.LargeBuilding, false, "", true, BuildingType.Interior, 0, 0, 0)]
        LargeStyle2Interior = 13,
        [BuildingStyle("Small Style 3 - Interior", "house_int_7", BaseStructure.SmallBuilding, false, "", true, BuildingType.Interior, 0, 0, 0)]
        SmallStyle3Interior = 14,
        [BuildingStyle("Medium Style 3 - Interior", "house_int_8", BaseStructure.MediumBuilding, false, "", true, BuildingType.Interior, 0, 0, 0)]
        MediumStyle3Interior = 15,
        [BuildingStyle("Large Style 3 - Interior", "house_int_9", BaseStructure.LargeBuilding, false, "", true, BuildingType.Interior, 0, 0, 0)]
        LargeStyle3Interior = 16,
        [BuildingStyle("Small Style 4 - Interior", "house_int_10", BaseStructure.SmallBuilding, false, "", true, BuildingType.Interior, 0, 0, 0)]
        SmallStyle4Interior = 17,
        [BuildingStyle("Medium Style 4 - Interior", "house_int_11", BaseStructure.MediumBuilding, false, "", true, BuildingType.Interior, 0, 0, 0)]
        MediumStyle4Interior = 18,
        [BuildingStyle("Large Style 4 - Interior", "house_int_12", BaseStructure.LargeBuilding, false, "", true, BuildingType.Interior, 0, 0, 0)]
        LargeStyle4Interior = 19,
        [BuildingStyle("Light Freighter 1", "starship1", BaseStructure.StarshipLightFreighter1, true, "Starship1Rule", true, BuildingType.Exterior, 0, 0, 0)]
        LightFreighter1 = 20,
        [BuildingStyle("Light Escort 1", "starship2", BaseStructure.StarshipLightEscort1, true, "Starship2Rule", true, BuildingType.Exterior, 0, 0, 0)]
        LightEscort1 = 21,
        [BuildingStyle("Light Freighter 1 Interior", "starship1_int", BaseStructure.StarshipLightFreighter1, true, "", true, BuildingType.Starship, 0, 0, 60)]
        LightFreighter1Interior = 22,
        [BuildingStyle("Light Escort 1 Interior", "starship2_int", BaseStructure.StarshipLightEscort1, true, "", true, BuildingType.Starship, 0, 0, 45)]
        LightEscort1Interior = 23,
    }

    public class BuildingStyleAttribute: Attribute
    {
        public string Name { get; set; }
        public string Resref { get; set; }
        public BaseStructure BaseStructureID { get; set; }
        public bool IsDefault { get; set; }
        public string DoorRule { get; set; }
        public bool IsActive { get; set; }
        public BuildingType BuildingTypeID { get; set; }
        public int PurchasePrice { get; set; }
        public int DailyUpkeep { get; set; }
        public int FurnitureLimit { get; set; }

        public BuildingStyleAttribute(
            string name, 
            string resref, 
            BaseStructure baseStructureID, 
            bool isDefault, 
            string doorRule,
            bool isActive,
            BuildingType buildingTypeID,
            int purchasePrice,
            int dailyUpkeep,
            int furnitureLimit)
        {
            Name = name;
            Resref = resref;
            BaseStructureID = baseStructureID;
            IsDefault = isDefault;
            DoorRule = doorRule;
            IsActive = isActive;
            BuildingTypeID = buildingTypeID;
            PurchasePrice = purchasePrice;
            DailyUpkeep = dailyUpkeep;
            FurnitureLimit = furnitureLimit;
        }
    }
}
