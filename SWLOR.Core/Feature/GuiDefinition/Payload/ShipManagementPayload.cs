using SWLOR.Core.Enumeration;
using SWLOR.Core.Service.GuiService;

namespace SWLOR.Core.Feature.GuiDefinition.Payload
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
