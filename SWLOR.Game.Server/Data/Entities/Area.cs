using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class Area
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Area()
        {
            AreaWalkmeshes = new HashSet<AreaWalkmesh>();
        }

        [StringLength(60)]
        public string AreaID { get; set; }

        [Required]
        [StringLength(16)]
        public string Resref { get; set; }

        [Required]
        [StringLength(128)]
        public string Name { get; set; }

        [Required]
        [StringLength(32)]
        public string Tag { get; set; }

        public int ResourceSpawnTableID { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public bool IsBuildable { get; set; }

        [StringLength(60)]
        public string NorthwestOwner { get; set; }

        [StringLength(60)]
        public string NortheastOwner { get; set; }

        [StringLength(60)]
        public string SouthwestOwner { get; set; }

        [StringLength(60)]
        public string SoutheastOwner { get; set; }

        public bool IsActive { get; set; }

        public int PurchasePrice { get; set; }

        public int DailyUpkeep { get; set; }

        public string Walkmesh { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime DateLastBaked { get; set; }

        public bool AutoSpawnResources { get; set; }

        public int ResourceQuality { get; set; }

        public virtual PlayerCharacter NortheastOwnerPlayer { get; set; }

        public virtual PlayerCharacter NorthwestOwnerPlayer { get; set; }

        public virtual PlayerCharacter SoutheastOwnerPlayer { get; set; }

        public virtual PlayerCharacter SouthwestOwnerPlayer { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<AreaWalkmesh> AreaWalkmeshes { get; set; }
    }
}
