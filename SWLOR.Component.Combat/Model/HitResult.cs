using SWLOR.Component.Combat.Enums;

namespace SWLOR.Component.Combat.Model
{
    internal struct HitResult
    {
        public HitType HitType { get; set; }
        public int HitRate { get; set; }
    }
}
