using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("PCGuildPoint")]
    public class PCGuildPoint: IEntity
    {
        public PCGuildPoint()
        {
            ID = Guid.NewGuid();
        }

        [ExplicitKey]
        public Guid ID { get; set; }
        public Guid PlayerID { get; set; }
        public int GuildID { get; set; }
        public int Rank { get; set; }
        public int Points { get; set; }

        public IEntity Clone()
        {
            return new PCGuildPoint
            {
                ID = ID,
                PlayerID = PlayerID,
                GuildID = GuildID,
                Rank = Rank,
                Points = Points
            };
        }
    }
}
