using System.Collections.Generic;
using SWLOR.Game.Server.Service.EdictService;

namespace SWLOR.Game.Server.Feature.EdictDefinition
{
    public class CraftingEdictDefinition : IEdictListDefinition
    {
        private readonly EdictBuilder _builder = new();

        public Dictionary<EdictType, EdictDetail> BuildEdicts()
        {
            ArtisansCharter();
            BreedersCharter();

            return _builder.Build();
        }

        private void ArtisansCharter()
        {
            _builder.Create(EdictType.ArtisansCharter, EdictPillarType.Crafting)
                .Name("Artisans' Charter")
                .Description("Citizens earn +10% crafting XP at stations inside the city, including houses built there.")
                .WeeklyCost(10000);
        }

        private void BreedersCharter()
        {
            _builder.Create(EdictType.BreedersCharter, EdictPillarType.Crafting)
                .Name("Breeders' Charter")
                .Description("Citizens' incubation stages in the city's Labs raise each purity stat 10% more, even past the 10% stage cap.")
                .WeeklyCost(10000);
        }
    }
}
