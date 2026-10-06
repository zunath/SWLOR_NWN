using System.Collections.Generic;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.SpaceObjectDefinition
{
    public sealed class NPCSpaceObjectDefinition : ISpaceObjectListDefinition
    {
        public Dictionary<string, SpaceObjectDetail> BuildSpaceObjects()
        {
            var builder = new SpaceObjectBuilder();
            foreach (var binding in SpaceEncounterCatalog.Default.Bindings.Values)
                builder.Create(binding.Tag).ItemTag(binding.Ship).EncounterProfile(binding.Profile);
            return builder.Build();
        }
    }
}
