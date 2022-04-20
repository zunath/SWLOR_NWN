using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public interface ISpaceObjectListDefinition
    {
        public Dictionary<string, SpaceObjectDetail> BuildSpaceObjects();
    }
}
