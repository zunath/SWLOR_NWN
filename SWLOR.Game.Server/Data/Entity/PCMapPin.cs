

using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PCMapPin]")]
    public class PCMapPin: IEntity
    {
        public PCMapPin()
        {
            ID = Guid.NewGuid();
        }
        [ExplicitKey]
        public Guid ID { get; set; }
        public Guid PlayerID { get; set; }
        public string AreaTag { get; set; }
        public double PositionX { get; set; }
        public double PositionY { get; set; }
        public string NoteText { get; set; }

        public IEntity Clone()
        {
            return new PCMapPin
            {
                ID = ID,
                PlayerID = PlayerID,
                AreaTag = AreaTag,
                PositionX = PositionX,
                PositionY = PositionY,
                NoteText = NoteText
            };
        }
    }
}
