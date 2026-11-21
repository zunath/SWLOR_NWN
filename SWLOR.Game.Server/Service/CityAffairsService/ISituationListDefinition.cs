using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CityAffairsService
{
    public interface ISituationListDefinition
    {
        public Dictionary<SituationType, SituationDetail> Build();
    }
}
