
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PCMapPins]")]
    public class PCMapPin: IEntity
    {
        [Key]
        public int PCMapPinID { get; set; }
        public string PlayerID { get; set; }
        public string AreaTag { get; set; }
        public double PositionX { get; set; }
        public double PositionY { get; set; }
        public string NoteText { get; set; }
    }
}
