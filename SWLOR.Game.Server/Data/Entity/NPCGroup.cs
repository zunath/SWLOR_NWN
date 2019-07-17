using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[NPCGroup]")]
    public class NPCGroup: IEntity
    {
        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }

        public IEntity Clone()
        {
            return new NPCGroup
            {
                ID = ID,
                Name = Name
            };
        }
    }
}
