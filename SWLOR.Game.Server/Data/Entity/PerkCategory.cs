using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PerkCategory]")]
    public class PerkCategory: IEntity
    {
        public PerkCategory()
        {
            Name = "";
        }

        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public int Sequence { get; set; }
    }
}
