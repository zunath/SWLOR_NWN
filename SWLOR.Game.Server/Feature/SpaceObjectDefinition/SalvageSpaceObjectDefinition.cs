using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.SpaceObjectDefinition
{
    /// <summary>
    /// Gas clouds and wrecks: space placeables a ship targets by clicking, as it does asteroids (plan section 12).
    /// </summary>
    public class SalvageSpaceObjectDefinition : ISpaceObjectListDefinition
    {
        /// <summary>The tag every gas cloud blueprint shares.</summary>
        public const string GasCloudTag = "spc_gascloud";

        private readonly SpaceObjectBuilder _builder = new();

        public Dictionary<string, SpaceObjectDetail> BuildSpaceObjects()
        {
            // The item tag is unused for placeables; it matches the asteroid's.
            _builder.Create(GasCloudTag)
                .ItemTag("asteroid");

            _builder.Create(Space.WreckTag)
                .ItemTag("asteroid");

            return _builder.Build();
        }
    }
}
