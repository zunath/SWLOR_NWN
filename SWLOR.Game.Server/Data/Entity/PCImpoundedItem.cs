
using System;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("PCImpoundedItems")]
    public class PCImpoundedItem: IEntity
    {
        [Key]
        public int PCImpoundedItemID { get; set; }
        public string PlayerID { get; set; }
        public string ItemName { get; set; }
        public string ItemTag { get; set; }
        public string ItemResref { get; set; }
        public string ItemObject { get; set; }
        public DateTime DateImpounded { get; set; }
        public DateTime? DateRetrieved { get; set; }
    
    }
}
