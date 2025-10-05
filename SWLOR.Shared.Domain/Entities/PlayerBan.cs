using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Entities
{
    public class PlayerBan: EntityBase
    {
        public string CDKey { get; set; }
        public string Reason { get; set; }
    }
}
