namespace SWLOR.Game.Server.Domain.World
{
    public class FishDetail
    {
        public FishType Type { get; set; }
        public int Frequency { get; set; }
        public FishTimeOfDayType TimeOfDay { get; set; }
    }
}
