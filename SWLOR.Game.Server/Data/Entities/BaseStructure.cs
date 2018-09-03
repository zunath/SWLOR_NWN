using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class BaseStructure
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int BaseStructureID { get; set; }

        public int BaseStructureTypeID { get; set; }

        [Required]
        [StringLength(64)]
        public string Name { get; set; }

        [Required]
        [StringLength(16)]
        public string PlaceableResref { get; set; }

        [Required]
        [StringLength(16)]
        public string ItemResref { get; set; }

        public bool IsActive { get; set; }

        public double Power { get; set; }

        public double CPU { get; set; }

        public int WeeklyUpkeepBase { get; set; }

        public int HitPoints { get; set; }

        public virtual BaseStructureType BaseStructureType { get; set; }
    }
}
