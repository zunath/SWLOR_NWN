using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[LootTable]")]
    public class LootTable: IEntity
    {
        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }

        public IEntity Clone()
        {
            return new LootTable
            {
                ID = ID,
                Name = Name
            };
        }
    }
}
