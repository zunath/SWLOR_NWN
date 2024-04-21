using SWLOR.Core.Service.SpaceService;

namespace SWLOR.Core.Feature.SpaceObjectDefinition
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