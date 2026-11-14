using System.Collections.Generic;
using SWLOR.Game.Server.Service.EdictService;

namespace SWLOR.Game.Server.Feature.EdictDefinition
{
    public class DiplomacyEdictDefinition : IEdictListDefinition
    {
        private readonly EdictBuilder _builder = new();

        public Dictionary<EdictType, EdictDetail> BuildEdicts()
        {
            Alliance();
            Embargo();

            return _builder.Build();
        }

        private void Alliance()
        {
            _builder.Create(EdictType.Alliance, EdictPillarType.Diplomacy)
                .Name("Alliance")
                .Description("Names one city. When both mayors enact it, each city's citizens can use the other's Bank and Starport.")
                .WeeklyCost(5000)
                .NamesTargetCity()
                .NotACitizenBenefit();
        }

        private void Embargo()
        {
            _builder.Create(EdictType.Embargo, EdictPillarType.Diplomacy)
                .Name("Embargo")
                .Description("Names one city. Its citizens are barred from this city's facilities, even under Open Gates.")
                .WeeklyCost(2500)
                .NamesTargetCity()
                .NotACitizenBenefit();
        }
    }
}
