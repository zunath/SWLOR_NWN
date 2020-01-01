using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    public class PCGuildPoint: IEntity
    {
        public PCGuildPoint()
        {
            ID = Guid.NewGuid();
        }

        [Key]
        public Guid ID { get; set; }
        public Guid PlayerID { get; set; }
        public int GuildID { get; set; }
        public int Rank { get; set; }
        public int Points { get; set; }
    }
}
