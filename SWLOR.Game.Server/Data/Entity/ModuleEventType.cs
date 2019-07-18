using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[ModuleEventType]")]
    public class ModuleEventType: IEntity
    {
        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }

        public IEntity Clone()
        {
            return new ModuleEventType
            {
                ID = ID,
                Name = Name
            };
        }
    }
}
