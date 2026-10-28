using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service.StationService
{
    /// <summary>
    /// A station-only building (plan section 25): a console that snaps to its reserved deck slot, with an existing scenery
    /// model beside it, crafted with Fabrication.
    /// </summary>
    public class StationBuildingDetail
    {
        public StructureType Structure { get; init; }
        public string Name { get; init; }
        public string ItemResref { get; init; }
        public RecipeType Recipe { get; init; }
        public int RecipeLevel { get; init; }
        public (string Resref, int Quantity)[] Components { get; init; }
        public StationDeckSlotType Slot { get; init; }
        public string ConsoleResref { get; init; }
        public string ModelResref { get; init; }
    }
}
