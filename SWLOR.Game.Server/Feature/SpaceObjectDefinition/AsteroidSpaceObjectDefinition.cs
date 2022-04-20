using System.Collections.Generic;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.SpaceObjectDefinition
{
    public class AsteroidSpaceObjectDefinition : ISpaceObjectListDefinition
    {
        private readonly SpaceObjectBuilder _builder = new();

        public Dictionary<string, SpaceObjectDetail> BuildSpaceObjects()
        {
            Asteroid();

            return _builder.Build();
        }

        private void Asteroid()
        {
            _builder.Create("spc_asteroid")
                .ItemTag("asteroid");
        }

    }
}