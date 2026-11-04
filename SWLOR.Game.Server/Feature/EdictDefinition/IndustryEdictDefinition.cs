using System.Collections.Generic;
using SWLOR.Game.Server.Service.EdictService;

namespace SWLOR.Game.Server.Feature.EdictDefinition
{
    public class IndustryEdictDefinition : IEdictListDefinition
    {
        private readonly EdictBuilder _builder = new();

        public Dictionary<EdictType, EdictDetail> BuildEdicts()
        {
            IndustrialCharter();

            return _builder.Build();
        }

        private void IndustrialCharter()
        {
            _builder.Create(EdictType.IndustrialCharter, EdictPillarType.Industry)
                .Name("Industrial Charter")
                .Description("Citizens' holdings on the city's planet roll setbacks half as often and need half the repair goods.")
                .WeeklyCost(5000)
                .MinimumCityLevel(3)
                .PlanetBased();
        }
    }
}
