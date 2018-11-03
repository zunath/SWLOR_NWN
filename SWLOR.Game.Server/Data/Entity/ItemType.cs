
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("ItemTypes")]
    public class ItemType: IEntity
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public ItemType()
        {
            Name = "";
        }

        [ExplicitKey]
        public int ItemTypeID { get; set; }
        public string Name { get; set; }
    }
}
