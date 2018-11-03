
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("CooldownCategories")]
    public class CooldownCategory: IEntity
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public CooldownCategory()
        {
            Name = "";
        }

        [ExplicitKey]
        public int CooldownCategoryID { get; set; }
        public string Name { get; set; }
        public double BaseCooldownTime { get; set; }
    }
}
