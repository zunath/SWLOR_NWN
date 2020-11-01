using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Legacy.Data.Contracts;

namespace SWLOR.Game.Server.Legacy.Data.Entity
{
    [Table("SpaceEncounter")]
    public class SpaceEncounter: IEntity
    {
        [ExplicitKey]
        public int ID { get; set; }
        public string Planet { get; set; }
        public int TypeID { get; set; }
        public int Chance { get; set; }
        public int Difficulty { get; set; }
        public int LootTable { get; set; }

        public IEntity Clone()
        {
            return new SpaceEncounter
            {
                ID = ID,
                Planet = Planet,
                TypeID = TypeID,
                Chance = Chance,
                Difficulty = Difficulty,
                LootTable = LootTable
            };
        }
    }
}
