using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.ImplantService
{
    public interface IImplantListDefinition
    {
        public Dictionary<string, ImplantDetail> BuildImplants();
    }
}
