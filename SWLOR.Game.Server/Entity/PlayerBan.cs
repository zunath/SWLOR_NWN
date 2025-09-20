using SWLOR.Shared.Abstractions;

namespace SWLOR.Game.Server.Entity
{
    public class PlayerBan: EntityBase
    {
        public string CDKey { get; set; }
        public string Reason { get; set; }
    }
}
