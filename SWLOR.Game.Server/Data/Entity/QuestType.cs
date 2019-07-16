using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[QuestType]")]
    public class QuestType: IEntity
    {
        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }

        public QuestType Clone()
        {
            return new QuestType
            {
                ID = ID,
                Name = Name
            };
        }
    }
}
