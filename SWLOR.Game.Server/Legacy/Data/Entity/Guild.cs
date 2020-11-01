using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Legacy.Data.Contracts;

namespace SWLOR.Game.Server.Legacy.Data.Entity
{
    [Table("Guild")]
    public class Guild: IEntity
    {
        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public IEntity Clone()
        {
            return new Guild
            {
                ID = ID,
                Name = Name,
                Description = Description
            };
        }
    }
}
