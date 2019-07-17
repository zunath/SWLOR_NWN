using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[BaseItemType]")]
    public class BaseItemType: IEntity
    {
        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }

        public IEntity Clone()
        {
            return new BaseItemType
            {
                ID = ID,
                Name = Name
            };
        }
    }
}
