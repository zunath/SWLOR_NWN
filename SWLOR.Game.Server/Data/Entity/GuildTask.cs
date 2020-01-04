using System;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Data.Entity
{
    public class GuildTask: IEntity
    {
        [Key]
        public int ID { get; set; }
        public GuildType GuildID { get; set; }
        public int QuestID { get; set; }
        public int RequiredRank { get; set; }
        public bool IsCurrentlyOffered { get; set; }
    }
}
