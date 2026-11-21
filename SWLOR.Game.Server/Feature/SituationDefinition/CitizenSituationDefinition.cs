using System.Collections.Generic;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.SituationDefinition
{
    /// <summary>City Affairs situations that start with the city's citizens and visitors.</summary>
    public class CitizenSituationDefinition : ISituationListDefinition
    {
        /// <summary>The name of the Open Gates Edict, as an <c>EdictType</c> member.</summary>
        public const string OpenGatesEdict = "OpenGates";

        private readonly SituationBuilder _builder = new();

        public Dictionary<SituationType, SituationDetail> Build()
        {
            FestivalPetition();
            MedicalSupplyShortage();
            VisitorBrawl();
            VisitorsComplain();

            return _builder.Build();
        }

        private void FestivalPetition()
        {
            _builder.Create(SituationType.FestivalPetition)
                .Title("Festival Petition")
                .Text("Citizens have petitioned the city to hold a festival in the main square. Funding it would lift spirits across the city.")

                .AddChoice("Fund the festival")
                .Pay(10000)
                .Modifier(CityModifierType.FestivalXpBonus)
                .Gazette("Funded a city festival.")

                .AddChoice("Decline")
                .Gazette("Declined a petition to fund a festival.")

                .IgnoredGazette("The city ignored a petition for a festival.");
        }

        private void MedicalSupplyShortage()
        {
            _builder.Create(SituationType.MedicalSupplyShortage)
                .Title("Medical Supply Shortage")
                .Text("The Medical Center is running low on bacta and bandages. The staff can restock at a price, or ration what is left.")
                .Requires(SituationRequirement.WithFacility(PropertyUpgradeType.MedicalCenterLevel))

                .AddChoice("Buy supplies")
                .Pay(10000)
                .Gazette("Bought medical supplies to restock the Medical Center.")

                .AddChoice("Ration supplies")
                .Modifier(CityModifierType.MedicalCenterBonusHalved)
                .Gazette("Ordered the Medical Center to ration its supplies.")

                .IgnoredModifier(CityModifierType.MedicalCenterBonusHalved)
                .IgnoredGazette("The city ignored a medical supply shortage, and the Medical Center is rationing.");
        }

        private void VisitorBrawl()
        {
            _builder.Create(SituationType.VisitorBrawl)
                .Title("Visitor Brawl")
                .Text("A brawl between visitors has wrecked a street in the open city, and the guards are stretched thin.")
                .Requires(SituationRequirement.ActiveEdict(OpenGatesEdict))

                .AddChoice("Fine the visitors")
                .Gain(5000)
                .FollowUp(SituationType.VisitorsComplain, 1)
                .Gazette("Fined the visitors who started a brawl.")

                .AddChoice("Close the gates")
                .Modifier(CityModifierType.OpenGatesSuspended)
                .Gazette("Closed the city gates to outsiders after a brawl.")

                .IgnoredFollowUp(SituationType.VisitorsComplain, 1)
                .IgnoredGazette("The city ignored a brawl between visitors.");
        }

        private void VisitorsComplain()
        {
            _builder.Create(SituationType.VisitorsComplain)
                .Title("Visitors Complain")
                .Text("Visitors are complaining about how the city handled the brawl, and some are threatening to stay away.")
                .FollowUpOnly()

                .AddChoice("Apologize publicly")
                .Gazette("Apologized to visitors for how the city handled a brawl.")

                .AddChoice("Refund the fines")
                .Pay(5000)
                .Gazette("Refunded the fines of visitors who complained.")

                .IgnoredModifier(CityModifierType.OpenGatesSuspended)
                .IgnoredGazette("The city ignored visitors' complaints, and they have stopped coming through the gates.");
        }
    }
}
