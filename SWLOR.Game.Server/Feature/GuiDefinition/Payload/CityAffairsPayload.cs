using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    public class CityAffairsPayload : GuiPayloadBase
    {
        /// <summary>The property id of the city (or station) whose situations the window shows.</summary>
        public string CityId { get; set; }

        public CityAffairsPayload(string cityId)
        {
            CityId = cityId;
        }
    }
}
