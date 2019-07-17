using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[FameRegion]")]
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
