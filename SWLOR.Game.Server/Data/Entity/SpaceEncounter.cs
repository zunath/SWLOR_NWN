using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    public class SpaceEncounter: IEntity
    {
        [Key]
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
