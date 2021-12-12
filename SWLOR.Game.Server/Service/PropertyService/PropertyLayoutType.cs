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
            0,
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
            0,
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
            0,
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
            0,
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
            0,
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
            0,
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
            0,
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
            0,
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
            0,
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
            0,
            "starship2_int",
            true)]
        LightEscort1 = 11,

        [PropertyLayoutType(
            PropertyType.City,
            "City",
            50, // For Cities, determines how many regular structures (non-buildings) can be placed in the area
            0, 
            21,
            100000,
            5000,
            "",
            true)]
        City = 92,

        [PropertyLayoutType(
            PropertyType.CityHall,
            "City Hall",
            30,
            0,
            0,
            0,
            0,
            "city_hall", // todo: update to the new interior
            true)]
        CityHall = 93,

        [PropertyLayoutType(
            PropertyType.Bank,
            "Bank",
            30,
            0,
            0,
            0,
            0,
            "house_int_3", // todo: update to the new interior
            true)]
        Bank = 94,

        [PropertyLayoutType(
            PropertyType.MedicalCenter,
            "Medical Center",
            30,
            0,
            0,
            0,
            0,
            "house_int_3", // todo: update to the new interior
            true)]
        MedicalCenter = 95,

        [PropertyLayoutType(
            PropertyType.Starport,
            "Starport",
            30,
            0,
            0,
            0,
            0,
            "house_int_3", // todo: update to the new interior
            true)]
        Starport = 96,

        [PropertyLayoutType(
            PropertyType.Cantina,
            "Cantina",
            30,
            0,
            0,
            0,
            0,
            "house_int_3", // todo: update to the new interior
            true)]
        Cantina = 97,

        [PropertyLayoutType(
            PropertyType.House,
            "Small House - Style 1",
            55,
            40,
            0,
            0,
            0,
            "house_int_1", 
            true)]
        SmallHouseStyle1 = 98,

        [PropertyLayoutType(
            PropertyType.House,
            "Small House - Style 2",
            55,
            40,
            0,
            0,
            0,
            "house_int_4",
            true)]
        SmallHouseStyle2 = 99,

        [PropertyLayoutType(
            PropertyType.House,
            "Small House - Style 3",
            55,
            40,
            0,
            0,
            0,
            "house_int_7",
            true)]
        SmallHouseStyle3 = 100,

        [PropertyLayoutType(
            PropertyType.House,
            "Small House - Style 4",
            55,
            40,
            0,
            0,
            0,
            "house_int_10", 
            true)]
        SmallHouseStyle4 = 101,


        [PropertyLayoutType(
            PropertyType.House,
            "Medium House - Style 1",
            65,
            60,
            0,
            0,
            0,
            "house_int_2",
            true)]
        MediumHouseStyle1 = 102,

        [PropertyLayoutType(
            PropertyType.House,
            "Medium House - Style 2",
            65,
            60,
            0,
            0,
            0,
            "house_int_5",
            true)]
        MediumHouseStyle2 = 103,

        [PropertyLayoutType(
            PropertyType.House,
            "Medium House - Style 3",
            65,
            60,
            0,
            0,
            0,
            "house_int_8",
            true)]
        MediumHouseStyle3 = 104,

        [PropertyLayoutType(
            PropertyType.House,
            "Medium House - Style 4",
            65,
            60,
            0,
            0,
            0,
            "house_int_11",
            true)]
        MediumHouseStyle4 = 105,


        [PropertyLayoutType(
            PropertyType.House,
            "Large House - Style 1",
            75,
            80,
            0,
            0,
            0,
            "house_int_3",
            true)]
        LargeHouseStyle1 = 106,

        [PropertyLayoutType(
            PropertyType.House,
            "Large House - Style 2",
            75,
            80,
            0,
            0,
            0,
            "house_int_6",
            true)]
        LargeHouseStyle2 = 107,

        [PropertyLayoutType(
            PropertyType.House,
            "Large House - Style 3",
            75,
            80,
            0,
            0,
            0,
            "house_int_9",
            true)]
        LargeHouseStyle3 = 108,

        [PropertyLayoutType(
            PropertyType.House,
            "Large House - Style 4",
            75,
            80,
            0,
            0,
            0,
            "house_int_12",
            true)]
        LargeHouseStyle4 = 109,


    }

    public class PropertyLayoutTypeAttribute : Attribute
    {
        public PropertyType PropertyType { get; set; }
        public string Name { get; set; }
        public int StructureLimit { get; set; }
        public int ItemStorageLimit { get; set; }
        public int BuildingLimit { get; set; }
        public bool IsActive { get; set; }
        public int InitialPrice { get; set; }
        public int PricePerDay { get; set; }
        public string AreaInstanceResref { get; set; }

        public PropertyLayoutTypeAttribute(
            PropertyType propertyType,
            string name,
            int structureLimit,
            int itemStorageLimit,
            int buildingLimit,
            int initialPrice,
            int pricePerDay,
            string areaInstanceResref,
            bool isActive)
        {
            PropertyType = propertyType;
            Name = name;
            StructureLimit = structureLimit;
            ItemStorageLimit = itemStorageLimit;
            BuildingLimit = buildingLimit;
            InitialPrice = initialPrice;
            PricePerDay = pricePerDay;
            AreaInstanceResref = areaInstanceResref;
            IsActive = isActive;
        }
    }
}
