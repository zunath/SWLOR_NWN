using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[GameTopicCategory]")]
    public class GameTopicCategory: IEntity
    {
        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }

        public IEntity Clone()
        {
            return new GameTopicCategory
            {
                ID = ID,
                Name = Name
            };
        }
    }
}
