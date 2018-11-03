using System;
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("BuildingStyles")]
    public class BuildingStyle: IEntity
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public BuildingStyle()
        {
            Name = "";
            Resref = "";
            DoorRule = "";
        }

        [ExplicitKey]
        public int BuildingStyleID { get; set; }
        public string Name { get; set; }
        public string Resref { get; set; }
        public int? BaseStructureID { get; set; }
        public bool IsDefault { get; set; }
        public string DoorRule { get; set; }
        public bool IsActive { get; set; }
        public int BuildingTypeID { get; set; }
        public int PurchasePrice { get; set; }
        public int DailyUpkeep { get; set; }
        public int FurnitureLimit { get; set; }
    }
}
