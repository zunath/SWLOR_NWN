using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.EdictService
{
    public interface IEdictListDefinition
    {
        public Dictionary<EdictType, EdictDetail> BuildEdicts();
    }
}
