using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[ItemType]")]
    public class ItemType: IEntity
    {
        public ItemType()
        {
            Name = "";
        }

        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }

        public IEntity Clone()
        {
            return new ItemType
            {
                ID = ID,
                Name = Name
            };
        }
    }
}
