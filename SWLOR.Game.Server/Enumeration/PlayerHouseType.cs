using System;

namespace SWLOR.Game.Server.Enumeration
{
    public enum PlayerHouseType
    {
        [PlayerHouse("Invalid", 0, 100, 0, "", false)]
        Invalid = 0,
        [PlayerHouse("Basic - Wooden/White (4x4)", 30, 1, 5000, "player_layout_1", true)]
        Basic = 1,
        [PlayerHouse("Advanced - Wooden/White (6x6)", 50, 3, 15000, "player_layout_2", true)]
        Advanced = 2,
        [PlayerHouse("Superior - Wooden/White (8x8)", 70, 6, 30000, "player_layout_3", true)]
        Superior = 3,
        [PlayerHouse("Master - Wooden/White (10x10)", 100, 10, 50000, "player_layout_4", true)]
        Master = 4
    }

    public class PlayerHouseAttribute : Attribute
    {
        public string Name { get; set; }
        public int FurnitureLimit { get; set; }
        public bool IsActive { get; set; }
        public int RequiredSeedRank { get; set; }
        public int Price { get; set; }
        public string AreaInstanceResref { get; set; }

        public PlayerHouseAttribute(
            string name, 
            int furnitureLimit, 
            int requiredSeedRank,
            int price,
            string areaInstanceResref,
            bool isActive)
        {
            Name = name;
            FurnitureLimit = furnitureLimit;
            RequiredSeedRank = requiredSeedRank;
            Price = price;
            AreaInstanceResref = areaInstanceResref;
            IsActive = isActive;
        }
    }
}
