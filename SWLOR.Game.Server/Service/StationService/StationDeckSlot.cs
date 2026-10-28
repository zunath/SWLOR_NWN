using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service.StationService
{
    /// <summary>A slot on the station deck. <see cref="Building"/> is Invalid for a slot kept clear for a later building.</summary>
    public class StationDeckSlot
    {
        public StationDeckSlotType Type { get; init; }
        public float X { get; init; }
        public float Y { get; init; }
        public StructureType Building { get; init; }
    }
}
