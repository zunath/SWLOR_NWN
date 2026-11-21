using System.Collections.Generic;
using SWLOR.Game.Server.Service.CityAffairsService;

namespace SWLOR.Game.Server.Feature.SituationDefinition
{
    /// <summary>City Affairs situations on the roads outside a planet city.</summary>
    public class RoadSituationDefinition : ISituationListDefinition
    {
        private readonly SituationBuilder _builder = new();

        public Dictionary<SituationType, SituationDetail> Build()
        {
            BeastSighting();
            CaravanAttacked();

            return _builder.Build();
        }

        private void BeastSighting()
        {
            _builder.Create(SituationType.BeastSighting)
                .Title("Beast Sighting")
                .Text("Travelers report a dangerous beast prowling the trade road near the city, and merchants are nervous.")
                .Requires(SituationRequirement.PlanetCity())

                .AddChoice("Post a bounty")
                .Pay(5000)
                .Hook()
                .Gazette("Posted a bounty on a beast near the city.")

                .AddChoice("Ignore it")
                .FollowUp(SituationType.CaravanAttacked, 3)
                .Hook()
                .Gazette("Dismissed reports of a beast near the city.")

                .IgnoredFollowUp(SituationType.CaravanAttacked, 3)
                .IgnoredHook()
                .IgnoredGazette("The city ignored reports of a beast near the trade road.");
        }

        private void CaravanAttacked()
        {
            _builder.Create(SituationType.CaravanAttacked)
                .Title("Caravan Attacked")
                .Text("A trade caravan bound for the city was attacked on the road. The survivors have reached the gates and ask what the city will do.")
                .FollowUpOnly()

                .AddChoice("Pay for the lost cargo")
                .Pay(10000)
                .Gazette("Paid merchants for the cargo lost when a caravan was attacked.")

                .AddChoice("Send an escort")
                .Pay(5000)
                .Gazette("Sent an escort to bring the survivors of an attacked caravan to safety.")

                .IgnoredModifier(CityModifierType.TransportationIncomeHalved)
                .IgnoredGazette("The city ignored an attacked caravan, and merchants are avoiding the road.");
        }
    }
}
