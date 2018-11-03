
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("ItemTypes")]
    public partial class ItemType: IEntity, ICacheable
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public ItemType()
        {
            this.Name = "";
        }

        [ExplicitKey]
        public int ItemTypeID { get; set; }
        public string Name { get; set; }
    }
}
