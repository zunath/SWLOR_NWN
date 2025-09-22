using SWLOR.Component.Crafting.Enums;

namespace SWLOR.Component.Crafting.Model
{
    public class FishDetail
    {
        public FishType Type { get; set; }
        public int Frequency { get; set; }
        public FishTimeOfDayType TimeOfDay { get; set; }
    }
}
