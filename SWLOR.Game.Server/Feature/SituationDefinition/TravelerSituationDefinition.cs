using System.Collections.Generic;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.TravelerService;

namespace SWLOR.Game.Server.Feature.SituationDefinition
{
    /// <summary>
    /// City Affairs situations a traveler brings. The exotic beast trader asks to keep animals in the plaza, and only does so
    /// in the week a city hosts him.
    /// </summary>
    public class TravelerSituationDefinition : ISituationListDefinition
    {
        private readonly SituationBuilder _builder = new();

        public Dictionary<SituationType, SituationDetail> Build()
        {
            BeastTraderPens();

            return _builder.Build();
        }

        // The pay and the gain sit inside the bands the other situations use: Pests in the Gardens costs 5,000, Festival
        // Petition 10,000, and Visitor Brawl gains 5,000. A choice that gains credits carries a penalty, so the trader's fee
        // for the space comes with pests that follow the animals.
        private void BeastTraderPens()
        {
            _builder.Create(SituationType.BeastTraderPens)
                .Title("Beasts in the Plaza")
                .Text("The exotic beast trader asks to keep a pen of young animals in the plaza for the week. Visitors would stop to look and he would pay for the space, but the pens draw strays and pests.")
                .Requires(SituationRequirement.TravelerHost(TravelerType.BeastTrader.ToString()))

                .AddChoice("Allow the pens")
                .Gain(3000)
                .Modifier(CityModifierType.CropPests)
                .Gazette("Allowed the exotic beast trader to keep a pen of animals in the plaza, and pests followed.")

                .AddChoice("Host an animal fair")
                .Pay(5000)
                .Modifier(CityModifierType.FestivalXpBonus)
                .Gazette("Paid for an animal fair around the beast trader's pens in the plaza.")

                .AddChoice("Turn them away")
                .Gazette("Turned the exotic beast trader's animals away from the plaza.")

                .IgnoredPays(2000)
                .IgnoredGazette("The city ignored the beast trader's request, and the animals left a mess in the plaza.");
        }
    }
}
