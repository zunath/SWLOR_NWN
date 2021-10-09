using System;

namespace SWLOR.Game.Server.Service.HousingService
{
    public enum PlayerHouseType
    {
        [PlayerHouse("Invalid", 0, 999999,  "", false)]
        Invalid = 0,
        [PlayerHouse("Small Apartment - Style 1", 30, 5000, "apartment_002", true)]
        SmallStyle1 = 1,
        [PlayerHouse("Small Apartment - Style 2", 30, 5000, "playerap_s_unf", true)]
        SmallStyle2 = 2,
        [PlayerHouse("Small Apartment - Style 2 (Furnished)", 30, 7000, "playerap_s_fur", true)]
        SmallStyle2Furnished = 3,
        [PlayerHouse("Medium Apartment - Style 1", 50, 10000, "apartment_2", true)]
        MediumStyle1 = 4,
        [PlayerHouse("Medium Apartment - Style 2", 50, 10000, "playerap_m_unf", true)]
        MediumStyle2 = 5,
        [PlayerHouse("Medium Apartment - Style 2 (Furnished)", 50, 13000, "playerap_m_fur", true)]
        MediumStyle2Furnished = 6,
        [PlayerHouse("Large Apartment - Style 1", 70, 25000, "apartment_3", true)]
        LargeStyle1 = 7,
        [PlayerHouse("Large Apartment - Style 2", 70, 25000, "playerap_l_unf", true)]
        LargeStyle2 = 8,
        [PlayerHouse("Large Apartment - Style 2 (Furnished)", 70, 30000, "playerap_l_fur", true)]
        LargeStyle2Furnished = 9,
    }

    public class PlayerHouseAttribute : Attribute
    {
        public string Name { get; set; }
        public int FurnitureLimit { get; set; }
        public bool IsActive { get; set; }
        public int Price { get; set; }
        public string AreaInstanceResref { get; set; }

        public PlayerHouseAttribute(
            string name, 
            int furnitureLimit, 
            int price,
            string areaInstanceResref,
            bool isActive)
        {
            Name = name;
            FurnitureLimit = furnitureLimit;
            Price = price;
            AreaInstanceResref = areaInstanceResref;
            IsActive = isActive;
        }
    }
}
