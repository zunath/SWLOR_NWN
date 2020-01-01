using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    public class ComponentType: IEntity
    {
        public ComponentType()
        {
            Name = "";
        }

        [Key]
        public int ID { get; set; }
        public string Name { get; set; }
        public string ReassembledResref { get; set; }
    }
}
