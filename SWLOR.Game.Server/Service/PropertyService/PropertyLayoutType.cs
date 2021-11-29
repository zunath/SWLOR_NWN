using System;

namespace SWLOR.Game.Server.Service.PropertyService
{
    public enum PropertyLayoutType
    {
        [PropertyLayoutType(
            PropertyType.Invalid,
            "Invalid", 
            0,
            0,
            999999,
            999999,
            "", 
            false)]
        Invalid = 0,
        [PropertyLayoutType(
            PropertyType.Apartment,
            "Small Apartment - Style 1",
            30, 
            25,
            5000, 
            200,
            "apartment_002", 
            true)]
        ApartmentSmallStyle1 = 1,
        [PropertyLayoutType(
            PropertyType.Apartment,
            "Small Apartment - Style 2", 
            30, 
            25,
            5000, 
            200,
            "playerap_s_unf", 
            true)]
        ApartmentSmallStyle2 = 2,
        [PropertyLayoutType(
            PropertyType.Apartment,
            "Small Apartment - Style 2 (Furnished)",
            15, 
            25,
            7000,
            300,
            "playerap_s_fur", 
            true)]
        ApartmentSmallStyle2Furnished = 3,
        [PropertyLayoutType(
            PropertyType.Apartment,
            "Medium Apartment - Style 1",
            50, 
            40,
            10000, 
            300,
            "apartment_2", 
            true)]
        ApartmentMediumStyle1 = 4,
        [PropertyLayoutType(
            PropertyType.Apartment,
            "Medium Apartment - Style 2", 
            50, 
            40,
            10000, 
            300,
            "playerap_m_unf", 
            true)]
        ApartmentMediumStyle2 = 5,
        [PropertyLayoutType(
            PropertyType.Apartment,
            "Medium Apartment - Style 2 (Furnished)", 
            25, 
            40,
            13000, 
            400,
            "playerap_m_fur", 
            true)]
        ApartmentMediumStyle2Furnished = 6,
        [PropertyLayoutType(
            PropertyType.Apartment,
            "Large Apartment - Style 1", 
            70, 
            80,
            25000, 
            800,
            "apartment_3", 
            true)]
        ApartmentLargeStyle1 = 7,
        [PropertyLayoutType(
            PropertyType.Apartment,
            "Large Apartment - Style 2", 
            70, 
            80,
            25000, 
            800,
            "playerap_l_unf", 
            true)]
        ApartmentLargeStyle2 = 8,
        [PropertyLayoutType(
            PropertyType.Apartment,
            "Large Apartment - Style 2 (Furnished)",
            35, 
            80,
            30000, 
            900,
            "playerap_l_fur", 
            true)]
        ApartmentLargeStyle2Furnished = 9,

        [PropertyLayoutType(
            PropertyType.Starship,
            "Light Freighter 1",
            30,
            25,
            0,
            0,
            "starship1_int",
            true)]
        LightFreighter1 = 10,

        [PropertyLayoutType(
            PropertyType.Starship,
            "Light Escort 1",
            30,
            25,
            0,
            0,
            "starship2_int",
            true)]
        LightEscort1 = 11,

        [PropertyLayoutType(
            PropertyType.CityHall,
            "City Hall",
            30,
            0,
            0,
            0,
            "house_int_3", // todo: update to the new city hall interior
            true)]
        CityHall = 98,

        [PropertyLayoutType(
            PropertyType.City,
            "City",
            50, // For Cities, determines how many regular structures (non-buildings) can be placed in the area
            20, // For Cities, determines how many buildings (non-structures) can be placed in the area
            100000,
            5000,
            "",
            true)]
        City = 99,

    }

    public class PropertyLayoutTypeAttribute : Attribute
    {
        public PropertyType PropertyType { get; set; }
        public string Name { get; set; }
        public int StructureLimit { get; set; }
        public int ItemStorageLimit { get; set; }
        public bool IsActive { get; set; }
        public int InitialPrice { get; set; }
        public int PricePerDay { get; set; }
        public string AreaInstanceResref { get; set; }

        public PropertyLayoutTypeAttribute(
            PropertyType propertyType,
            string name,
            int structureLimit,
            int itemStorageLimit,
            int initialPrice,
            int pricePerDay,
            string areaInstanceResref,
            bool isActive)
        {
            PropertyType = propertyType;
            Name = name;
            StructureLimit = structureLimit;
            ItemStorageLimit = itemStorageLimit;
            InitialPrice = initialPrice;
            PricePerDay = pricePerDay;
            AreaInstanceResref = areaInstanceResref;
            IsActive = isActive;
        }
    }
}
