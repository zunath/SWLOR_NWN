using System.Collections.Generic;
using SWLOR.Game.Server.Service.BeastMasteryService;

namespace SWLOR.Game.Server.Feature.BeastDefinition
{
    public class AradileBeastDefinition: IBeastListDefinition
    {
        private BeastBuilder _builder = new();

        public Dictionary<BeastType, BeastDetail> Build()
        {

            return _builder.Build();
        }
    }
}
