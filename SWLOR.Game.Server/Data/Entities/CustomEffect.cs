using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class CustomEffect
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public CustomEffect()
        {
            PCCustomEffects = new HashSet<PCCustomEffect>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long CustomEffectID { get; set; }

        [Required]
        [StringLength(32)]
        public string Name { get; set; }

        public int IconID { get; set; }

        [Required]
        [StringLength(64)]
        public string ScriptHandler { get; set; }

        [Required]
        [StringLength(64)]
        public string StartMessage { get; set; }

        [Required]
        [StringLength(64)]
        public string ContinueMessage { get; set; }

        [Required]
        [StringLength(64)]
        public string WornOffMessage { get; set; }

        public bool IsStance { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCCustomEffect> PCCustomEffects { get; set; }
    }
}
