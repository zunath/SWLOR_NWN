using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[Mod]")]
    public class Mod: IEntity
    {
        public Mod()
        {
            Name = "";
            Script = "";
        }

        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }
        public string Script { get; set; }
        public bool IsActive { get; set; }
    }
}
