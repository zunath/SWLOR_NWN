
using System;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("PCImpoundedItems")]
    public partial class PCImpoundedItem: IEntity
    {
        [Key]
        public int PCImpoundedItemID { get; set; }
        public string PlayerID { get; set; }
        public string ItemName { get; set; }
        public string ItemTag { get; set; }
        public string ItemResref { get; set; }
        public string ItemObject { get; set; }
        public System.DateTime DateImpounded { get; set; }
        public Nullable<System.DateTime> DateRetrieved { get; set; }
    
        public virtual PlayerCharacter PlayerCharacter { get; set; }
    }
}
