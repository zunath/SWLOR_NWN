using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("GuildTask")]
    public class GuildTask: IEntity
    {
        [Key]
        public int ID { get; set; }
        public int GuildID { get; set; }
        public int QuestID { get; set; }
        public int RequiredRank { get; set; }
        public bool IsCurrentlyOffered { get; set; }

        public IEntity Clone()
        {
            return new GuildTask
            {
                ID = ID,
                GuildID = GuildID,
                QuestID = QuestID,
                RequiredRank = RequiredRank,
                IsCurrentlyOffered = IsCurrentlyOffered
            };
        }
    }
}
