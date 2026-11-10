using System.Collections.Generic;

namespace SWLOR.Game.Server.Domain.Space
{
    public interface ISpaceObjectListDefinition
    {
        public Dictionary<string, SpaceObjectDetail> BuildSpaceObjects();
    }
}
