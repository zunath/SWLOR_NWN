
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("GrowingPlants")]
    public partial class GrowingPlant: IEntity, ICacheable
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public GrowingPlant()
        {
            this.LocationAreaTag = "";
        }

        [Key]
        public int GrowingPlantID { get; set; }
        public int PlantID { get; set; }
        public int RemainingTicks { get; set; }
        public string LocationAreaTag { get; set; }
        public double LocationX { get; set; }
        public double LocationY { get; set; }
        public double LocationZ { get; set; }
        public double LocationOrientation { get; set; }
        public System.DateTime DateCreated { get; set; }
        public bool IsActive { get; set; }
        public int TotalTicks { get; set; }
        public int WaterStatus { get; set; }
        public int LongevityBonus { get; set; }
    
        public virtual Plant Plant { get; set; }
    }
}
