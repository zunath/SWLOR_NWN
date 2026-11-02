using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    /// <summary>Opens the Training Store window in its city mode, as the Civic Exchange of a city (Player City Revamp).</summary>
    public class CivicExchangePayload : GuiPayloadBase
    {
        /// <summary>The property id of the city whose Cantina stocks the Exchange.</summary>
        public string CityId { get; set; }

        public CivicExchangePayload(string cityId)
        {
            CityId = cityId;
        }
    }
}
