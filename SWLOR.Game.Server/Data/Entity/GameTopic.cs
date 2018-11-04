
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[GameTopics]")]
    public class GameTopic: IEntity
    {
        public GameTopic()
        {
            Icon = "";
        }

        [ExplicitKey]
        public int GameTopicID { get; set; }
        public string Name { get; set; }
        public string Text { get; set; }
        public int GameTopicCategoryID { get; set; }
        public bool IsActive { get; set; }
        public int Sequence { get; set; }
        public string Icon { get; set; }
    }
}
