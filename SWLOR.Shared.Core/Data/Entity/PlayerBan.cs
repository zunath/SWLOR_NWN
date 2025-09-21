using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Core.Data.Entity
{
    public class PlayerBan: EntityBase
    {
        public string CDKey { get; set; }
        public string Reason { get; set; }
    }
}
