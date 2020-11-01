using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Legacy.Data.Contracts;

namespace SWLOR.Game.Server.Legacy.Data.Entity
{
    [Table("FameRegion")]
    public class FameRegion: IEntity
    {
        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }

        public IEntity Clone()
        {
            return new FameRegion
            {
                ID = ID,
                Name = Name
            };
        }
    }
}
