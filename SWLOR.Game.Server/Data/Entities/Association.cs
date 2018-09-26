namespace SWLOR.Game.Server.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class Association
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Association()
        {
            PlayerCharacters = new HashSet<PlayerCharacter>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int AssociationID { get; set; }

        [Required]
        [StringLength(32)]
        public string Name { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PlayerCharacter> PlayerCharacters { get; set; }
    }
}
