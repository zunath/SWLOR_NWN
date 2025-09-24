using SWLOR.Component.Space.Contracts;
using SWLOR.Component.Space.Model;
using SWLOR.Component.Space.Service;

namespace SWLOR.Component.Space.Feature.SpaceObjectDefinition
{
    public class AsteroidSpaceObjectDefinition : ISpaceObjectListDefinition
    {
        private readonly ISpaceObjectBuilder _builder;

        public AsteroidSpaceObjectDefinition(ISpaceObjectBuilder builder)
        {
            _builder = builder;
        }

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
