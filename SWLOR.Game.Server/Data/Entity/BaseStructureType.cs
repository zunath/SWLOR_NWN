using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("BaseStructureType")]
    public partial class BaseStructureType: IEntity, ICacheable
    {

        public BaseStructureType()
        {
            this.Name = "";
            this.BaseStructures = new HashSet<BaseStructure>();
        }

        [ExplicitKey]
        public int BaseStructureTypeID { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public bool CanPlaceInside { get; set; }
        public bool CanPlaceOutside { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<BaseStructure> BaseStructures { get; set; }
    }
}
