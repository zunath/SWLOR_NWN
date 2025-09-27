using SWLOR.NWN.API.Engine;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.World.Enums;

namespace SWLOR.Shared.Domain.UI.Payloads
{
    public class ShipManagementPayload: IGuiPayload
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
