using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[Association]")]
    public class Association: IEntity
    {
        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }
    }
}
