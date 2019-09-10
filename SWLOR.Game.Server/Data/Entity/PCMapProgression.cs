

using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PCMapProgression]")]
    public class PCMapProgression: IEntity
    {
        public PCMapProgression()
        {
            ID = Guid.NewGuid();
        }
        [ExplicitKey]
        public Guid ID { get; set; }
        public Guid PlayerID { get; set; }
        public string AreaResref { get; set; }
        public string Progression { get; set; }

        public IEntity Clone()
        {
            return new PCMapProgression
            {
                ID = ID,
                PlayerID = PlayerID,
                AreaResref = AreaResref,
                Progression = Progression
            };
        }
    }
}
