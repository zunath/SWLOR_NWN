
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("CraftBlueprintCategories")]
    public class CraftBlueprintCategory: IEntity
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public CraftBlueprintCategory()
        {
        }

        [ExplicitKey]
        public long CraftBlueprintCategoryID { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
    
    }
}
