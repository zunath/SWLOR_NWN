using SWLOR.Shared.Abstractions;

namespace SWLOR.Component.Admin.Entity
{
    public class PlayerBan: EntityBase
    {
        public string CDKey { get; set; }
        public string Reason { get; set; }
    }
}
