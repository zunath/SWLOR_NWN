using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    public class GardenPlotPayload : GuiPayloadBase
    {
        /// <summary>The property id of the garden plot structure the window shows.</summary>
        public string PlotId { get; set; }

        public GardenPlotPayload(string plotId)
        {
            PlotId = plotId;
        }
    }
}
