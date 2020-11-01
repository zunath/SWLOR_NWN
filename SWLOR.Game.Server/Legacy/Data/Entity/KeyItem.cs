using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Legacy.Data.Contracts;

namespace SWLOR.Game.Server.Legacy.Data.Entity
{
    [Table("KeyItem")]
    public class KeyItem: IEntity
    {
        [ExplicitKey]
        public int ID { get; set; }
        public int KeyItemCategoryID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public IEntity Clone()
        {
            return new KeyItem
            {
                ID = ID,
                KeyItemCategoryID = KeyItemCategoryID,
                Name = Name,
                Description = Description
            };
        }
    }
}
