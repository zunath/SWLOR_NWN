

using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[Mods]")]
    public class Mod: IEntity
    {
        public Mod()
        {
            Name = "";
            Script = "";
        }

        [ExplicitKey]
        public int ModID { get; set; }
        public string Name { get; set; }
        public string Script { get; set; }
        public bool IsActive { get; set; }
    }
}
