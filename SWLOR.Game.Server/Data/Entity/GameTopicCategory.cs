using System.Collections.Generic;

using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[GameTopicCategories]")]
    public class GameTopicCategory: IEntity
    {
        [ExplicitKey]
        public int GameTopicCategoryID { get; set; }
        public string Name { get; set; }
    }
}
