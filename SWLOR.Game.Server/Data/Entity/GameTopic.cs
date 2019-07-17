using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[GameTopic]")]
    public class GameTopic: IEntity
    {
        public GameTopic()
        {
            Icon = "";
        }

        [ExplicitKey]
        public int ID { get; set; }
        public string Name { get; set; }
        public string Text { get; set; }
        public int GameTopicCategoryID { get; set; }
        public bool IsActive { get; set; }
        public int Sequence { get; set; }
        public string Icon { get; set; }

        public IEntity Clone()
        {
            return new GameTopic
            {
                ID = ID,
                Name = Name,
                Text = Text,
                GameTopicCategoryID = GameTopicCategoryID,
                IsActive = IsActive,
                Sequence = Sequence,
                Icon = Icon
            };
        }
    }
}
