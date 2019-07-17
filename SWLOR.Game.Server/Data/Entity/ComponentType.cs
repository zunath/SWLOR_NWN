using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[ComponentType]")]
    public class ComponentType: IEntity
    {
        public ComponentType()
        {
            Name = "";
        }

        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }
        public string ReassembledResref { get; set; }

        public IEntity Clone()
        {
            return new ComponentType
            {
                ID = ID,
                Name = Name,
                ReassembledResref = ReassembledResref
            };
        }
    }
}
