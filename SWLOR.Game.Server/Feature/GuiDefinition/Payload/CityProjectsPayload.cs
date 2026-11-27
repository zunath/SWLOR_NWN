using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    public class CityProjectsPayload : GuiPayloadBase
    {
        /// <summary>The property id of the city or station whose projects the window shows.</summary>
        public string CityId { get; set; }

        public CityProjectsPayload(string cityId)
        {
            CityId = cityId;
        }
    }
}
