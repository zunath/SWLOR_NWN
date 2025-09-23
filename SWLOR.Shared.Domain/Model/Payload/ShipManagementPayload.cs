using SWLOR.Component.World.Enums;
using SWLOR.NWN.API.Engine;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Shared.Domain.Model.Payload
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
