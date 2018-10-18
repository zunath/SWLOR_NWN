namespace SWLOR.Game.Server.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class PCCraftedBlueprint
    {
        public int PCCraftedBlueprintID { get; set; }

        [Required]
        [StringLength(60)]
        public string PlayerID { get; set; }

        public long CraftBlueprintID { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime DateFirstCrafted { get; set; }

        public virtual CraftBlueprint CraftBlueprint { get; set; }

        public virtual PlayerCharacter PlayerCharacter { get; set; }
    }
}
