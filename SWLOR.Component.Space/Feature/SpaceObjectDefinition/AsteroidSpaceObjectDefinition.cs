using SWLOR.Component.Space.Contracts;
using SWLOR.Component.Space.Model;
using SWLOR.Component.Space.Service;

namespace SWLOR.Component.Space.Feature.SpaceObjectDefinition
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
