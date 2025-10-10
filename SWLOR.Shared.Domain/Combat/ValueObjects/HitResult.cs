using SWLOR.Shared.Domain.Combat.Enums;

namespace SWLOR.Shared.Domain.Combat.ValueObjects
{
    public struct HitResult
    {
        public HitType HitType { get; set; }
        public int HitRate { get; set; }
    }
}
