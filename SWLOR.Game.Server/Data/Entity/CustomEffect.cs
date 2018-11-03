using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("CustomEffects")]
    public partial class CustomEffect: IEntity, ICacheable
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public CustomEffect()
        {
            this.PCCustomEffects = new HashSet<PCCustomEffect>();
        }

        [ExplicitKey]
        public long CustomEffectID { get; set; }
        public string Name { get; set; }
        public int IconID { get; set; }
        public string ScriptHandler { get; set; }
        public string StartMessage { get; set; }
        public string ContinueMessage { get; set; }
        public string WornOffMessage { get; set; }
        public int CustomEffectCategoryID { get; set; }
    
        public virtual CustomEffectCategory CustomEffectCategory { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCCustomEffect> PCCustomEffects { get; set; }
    }
}
