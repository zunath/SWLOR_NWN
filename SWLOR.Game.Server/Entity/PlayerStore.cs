using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Entity
{
    public class PlayerStore: EntityBase
    {
        public PlayerStore()
        {
            ItemsForSale = new Dictionary<string, PlayerStoreItem>();
            DateLeaseExpires = DateTime.UtcNow;
            IsOpen = false;
            TaxRate = 0.05f;
        }

        public string StoreName { get; set; }
        public Dictionary<string, PlayerStoreItem> ItemsForSale { get; set; }
        public override string KeyPrefix => "PlayerStore";
        public DateTime DateLeaseExpires { get; set; }
        public bool IsOpen { get; set; }
        public int Till { get; set; }
        public float TaxRate { get; set; }
    }

    public class PlayerStoreItem
    {
        public string Name { get; set; }
        public int Price { get; set; }
        public int StackSize { get; set; }
        public string Data { get; set; }
    }
}
