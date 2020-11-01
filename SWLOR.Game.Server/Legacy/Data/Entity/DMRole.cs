using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Legacy.Data.Contracts;

namespace SWLOR.Game.Server.Legacy.Data.Entity
{
    [Table("DMRole")]
    public class DMRole: IEntity
    {
        [ExplicitKey]
        public int ID { get; set; }
        public string Description { get; set; }

        public IEntity Clone()
        {
            return new DMRole
            {
                ID = ID,
                Description = Description
            };
        }
    }
}
