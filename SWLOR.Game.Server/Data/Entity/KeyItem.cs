using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Data.Entity
{
    public class KeyItem: IEntity
    {
        [Key]
        public int ID { get; set; }
        public KeyItemCategoryType KeyItemCategoryID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
