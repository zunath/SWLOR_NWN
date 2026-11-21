using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.SpaceObjectDefinition
{
    /// <summary>
    /// Gas clouds, wrecks, data cans and locked caches: space placeables a ship targets by clicking, as it does asteroids
    /// (plan sections 12 and 24). Only a locked cache has the click script, so a plain site cache is still used.
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

            _builder.Create(Space.DataCanTag)
                .ItemTag("asteroid");

            _builder.Create(ScanningTuning.CacheResref)
                .ItemTag("asteroid");

            return _builder.Build();
        }
    }
}
