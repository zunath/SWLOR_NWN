
using Dapper.Contrib.Extensions;

namespace SWLOR.Game.Server.Data
{
    using System;
    using System.Collections.Generic;
    
    using SWLOR.Game.Server.Data.Contracts;
    
    public partial class Attribute: IEntity
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Attribute()
        {
            this.Name = "";
            this.PrimarySkills = new HashSet<Skill>();
            this.SecondarySkills = new HashSet<Skill>();
            this.TertiarySkills = new HashSet<Skill>();
        }

        [ExplicitKey]
        public int AttributeID { get; set; }
        public int NWNValue { get; set; }
        public string Name { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Skill> PrimarySkills { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Skill> SecondarySkills { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Skill> TertiarySkills { get; set; }
    }
}
