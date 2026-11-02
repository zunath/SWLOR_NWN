using System.Collections.Generic;
using SWLOR.Game.Server.Service.EdictService;

namespace SWLOR.Game.Server.Feature.EdictDefinition
{
    public class EconomyEdictDefinition : IEdictListDefinition
    {
        private readonly EdictBuilder _builder = new();

        public Dictionary<EdictType, EdictDetail> BuildEdicts()
        {
            OpenGates();
            Shadowport();

            return _builder.Build();
        }

        private void OpenGates()
        {
            _builder.Create(EdictType.OpenGates, EdictPillarType.Economy)
                .Name("Open Gates")
                .Description("Non-citizens can use the Bank and Starport. Visitors pay the transportation tax into the treasury.")
                .WeeklyCost(5000)
                .NotACitizenBenefit();
        }

        private void Shadowport()
        {
            _builder.Create(EdictType.Shadowport, EdictPillarType.Economy)
                .Name("Shadowport")
                .Description("The Starport exchange buys contraband at close to black-market prices, with a small chance per sale of a customs bust. Normal goods sell 15% lower.")
                .WeeklyCost(10000)
                .NotACitizenBenefit();
        }
    }
}
