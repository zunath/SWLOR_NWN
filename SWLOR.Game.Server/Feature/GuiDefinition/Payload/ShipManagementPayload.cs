using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    public class ShipManagementPayload: GuiPayloadBase
    {
        public string SpecificPropertyId { get; set; }
        public PlanetType PlanetType { get; set; }
        public Location SpaceLocation { get; set; }
        public Location LandingLocation { get; set; }
        
        public ShipManagementPayload(PlanetType planetType, Location spaceLocation, Location landingLocation)
        {
            PlanetType = planetType;
            SpecificPropertyId = string.Empty;
            SpaceLocation = spaceLocation;
            LandingLocation = landingLocation;
        }

        public ShipManagementPayload(PlanetType planetType, string specificPropertyId)
        {
            PlanetType = planetType;
            SpecificPropertyId = specificPropertyId;
        }
    }
}
