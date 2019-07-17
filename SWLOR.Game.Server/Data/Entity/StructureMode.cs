using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[StructureMode]")]
    public class StructureMode: IEntity
    {
        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }

        public IEntity Clone()
        {
            return new StructureMode
            {
                ID = ID,
                Name = Name
            };
        }
    }
}
