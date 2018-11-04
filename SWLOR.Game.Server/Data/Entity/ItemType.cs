

using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[ItemTypes]")]
    public class ItemType: IEntity
    {
        public ItemType()
        {
            Name = "";
        }

        [ExplicitKey]
        public int ItemTypeID { get; set; }
        public string Name { get; set; }
    }
}
