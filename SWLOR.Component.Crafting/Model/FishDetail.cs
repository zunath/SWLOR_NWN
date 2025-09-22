using SWLOR.Shared.Core.Enums;

namespace SWLOR.Shared.Core.Models
{
    public class FishDetail
    {
        public FishType Type { get; set; }
        public int Frequency { get; set; }
        public FishTimeOfDayType TimeOfDay { get; set; }
    }
}
