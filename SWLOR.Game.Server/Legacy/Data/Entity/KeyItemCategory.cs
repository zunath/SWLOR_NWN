using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Legacy.Data.Contracts;

namespace SWLOR.Game.Server.Legacy.Data.Entity
{
    [Table("KeyItemCategory")]
    public class KeyItemCategory: IEntity
    {
        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }

        public IEntity Clone()
        {
            return new KeyItemCategory
            {
                ID = ID,
                Name = Name,
                IsActive = IsActive
            };
        }
    }
}
