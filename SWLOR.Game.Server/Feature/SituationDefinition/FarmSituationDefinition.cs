using System.Collections.Generic;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.SituationDefinition
{
    /// <summary>City Affairs situations on a city's farms: pests in the gardens, and predators at the corrals.</summary>
    public class FarmSituationDefinition : ISituationListDefinition
    {
        private readonly SituationBuilder _builder = new();

        public Dictionary<SituationType, SituationDetail> Build()
        {
            PestsInTheGardens();
            PredatorsAtTheCorrals();

            return _builder.Build();
        }

        private void PestsInTheGardens()
        {
            _builder.Create(SituationType.PestsInTheGardens)
                .Title("Pests in the Gardens")
                .Text("Pests are eating their way through the city's gardens. Exterminators can clear them out, or the farmers can put up with the losses. Crops keep growing either way.")
                .Requires(SituationRequirement.WithStructure(StructureType.GardenPlot))

                .AddChoice("Hire exterminators")
                .Pay(5000)
                .Gazette("Hired exterminators to clear pests from the city's gardens.")

                .AddChoice("Ignore it")
                .Modifier(CityModifierType.CropPests)
                .Gazette("Dismissed reports of pests in the city's gardens.")

                .IgnoredModifier(CityModifierType.CropPests)
                .IgnoredGazette("The city ignored pests in its gardens, and harvests are smaller.");
        }

        private void PredatorsAtTheCorrals()
        {
            _builder.Create(SituationType.PredatorsAtTheCorrals)
                .Title("Predators at the Corrals")
                .Text("Predators are prowling around the city's corrals. A hunt starts only when an official calls it: a pack attacks the corrals for 30 minutes, and the animals cannot be harmed. Drive the pack off, or the animals will give no products for 72 hours.")
                .Requires(SituationRequirement.PlanetCity())
                .Requires(SituationRequirement.WithStructure(StructureType.Corral))

                .AddChoice("Call a hunt")
                .CallHunt()
                .Gazette("Called a hunt to drive predators from the city's corrals.")

                .AddChoice("Ignore it")
                .LivestockLoseProducts()
                .Gazette("Dismissed reports of predators at the city's corrals.")

                .IgnoredLivestockLoseProducts()
                .IgnoredGazette("The city ignored predators at its corrals, and its animals give no products for 72 hours.");
        }
    }
}
