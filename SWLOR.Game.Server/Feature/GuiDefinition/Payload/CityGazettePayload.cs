using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    public class CityGazettePayload : GuiPayloadBase
    {
        /// <summary>The property id of the city (or station) whose Gazette the window shows.</summary>
        public string CityId { get; set; }

        public CityGazettePayload(string cityId)
        {
            CityId = cityId;
        }
    }
}
