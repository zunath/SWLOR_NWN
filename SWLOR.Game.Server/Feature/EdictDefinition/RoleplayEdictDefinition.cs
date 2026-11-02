using System.Collections.Generic;
using SWLOR.Game.Server.Service.EdictService;

namespace SWLOR.Game.Server.Feature.EdictDefinition
{
    public class RoleplayEdictDefinition : IEdictListDefinition
    {
        private readonly EdictBuilder _builder = new();

        public Dictionary<EdictType, EdictDetail> BuildEdicts()
        {
            FestivalSeason();

            return _builder.Build();
        }

        private void FestivalSeason()
        {
            _builder.Create(EdictType.FestivalSeason, EdictPillarType.Roleplay)
                .Name("Festival Season")
                .Description("Citizens earn 150 more RP XP per award (10% of its 1,500 base), on top of the Cantina.")
                .WeeklyCost(10000);
        }
    }
}
