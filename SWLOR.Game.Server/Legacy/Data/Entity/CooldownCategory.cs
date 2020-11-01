using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Legacy.Data.Contracts;

namespace SWLOR.Game.Server.Legacy.Data.Entity
{
    [Table("CooldownCategory")]
    public class CooldownCategory: IEntity
    {
        public CooldownCategory()
        {
            Name = "";
        }

        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }
        public double BaseCooldownTime { get; set; }

        public IEntity Clone()
        {
            return new CooldownCategory
            {
                ID = ID,
                Name = Name,
                BaseCooldownTime = BaseCooldownTime
            };
        }
    }
}
