using System.Collections.Generic;
using SWLOR.Game.Server.Service.EdictService;

namespace SWLOR.Game.Server.Feature.EdictDefinition
{
    public class PveEdictDefinition : IEdictListDefinition
    {
        private readonly EdictBuilder _builder = new();

        public Dictionary<EdictType, EdictDetail> BuildEdicts()
        {
            GuildCharter();
            ProspectorsCharter();
            DrydockSubsidy();
            HuntingGrounds();

            return _builder.Build();
        }

        private void GuildCharter()
        {
            _builder.Create(EdictType.GuildCharter, EdictPillarType.Pve)
                .Name("Guild Charter")
                .Description("Citizens earn +10% GP and credits from guild tasks.")
                .WeeklyCost(15000);
        }

        private void ProspectorsCharter()
        {
            _builder.Create(EdictType.ProspectorsCharter, EdictPillarType.Pve)
                .Name("Prospectors' Charter")
                .Description("Citizens harvesting on the city's planet get a separate 10% chance of 1 more item per harvest.")
                .WeeklyCost(10000)
                .PlanetBased();
        }

        private void DrydockSubsidy()
        {
            _builder.Create(EdictType.DrydockSubsidy, EdictPillarType.Pve)
                .Name("Drydock Subsidy")
                .Description("Citizens pay 15% less for ship repairs, on top of the Starport. The 90% cap stays.")
                .WeeklyCost(10000);
        }

        private void HuntingGrounds()
        {
            _builder.Create(EdictType.HuntingGrounds, EdictPillarType.Pve)
                .Name("Hunting Grounds")
                .Description("Citizens earn +10% XP from defeating enemies on the city's planet.")
                .WeeklyCost(15000)
                .MinimumCityLevel(3)
                .PlanetBased();
        }
    }
}
