using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("GuildTask")]
    public class GuildTask: IEntity
    {
        public GuildTask()
        {
            ID = Guid.NewGuid();
        }

        [ExplicitKey]
        public Guid ID { get; set; }
        public int GuildID { get; set; }
        public int QuestID { get; set; }
        public int RequiredRank { get; set; }
        public bool IsCurrentlyOffered { get; set; }
    }
}
