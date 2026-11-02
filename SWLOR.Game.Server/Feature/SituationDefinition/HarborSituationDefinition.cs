using System.Collections.Generic;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.SituationDefinition
{
    /// <summary>City Affairs situations that start at the starport: strikes, smugglers, and refugees.</summary>
    public class HarborSituationDefinition : ISituationListDefinition
    {
        private readonly SituationBuilder _builder = new();

        public Dictionary<SituationType, SituationDetail> Build()
        {
            DockWorkersStrike();
            SmugglersOffer();
            RefugeeShuttle();
            NewArrivals();

            return _builder.Build();
        }

        private void DockWorkersStrike()
        {
            _builder.Create(SituationType.DockWorkersStrike)
                .Title("Dock Workers Strike")
                .Text("The dock workers have walked off the job and want a raise. Cargo is piling up at the starport, and the harbor master wants an answer.")
                .Requires(SituationRequirement.CityLevel(2))
                .Requires(SituationRequirement.WithFacility(PropertyUpgradeType.StarportLevel))

                .AddChoice("Pay the raise")
                .Pay(15000)
                .Gazette("Paid the dock workers' raise and ended the strike.")

                .AddChoice("Refuse")
                .Modifier(CityModifierType.TransportationIncomeHalved)
                .Gazette("Refused the dock workers' demands. The idle docks will cut into transportation tax income.")

                .AddChoice("Negotiate")
                .ChoiceRequires(SituationRequirement.SkillRank(SkillType.Leadership, 10))
                .Pay(7500)
                .Gazette("Negotiated a settlement with the dock workers.")

                .IgnoredModifier(CityModifierType.TransportationIncomeHalved)
                .IgnoredGazette("The city ignored the dock workers' strike, and the idle docks cut into transportation tax income.");
        }

        private void SmugglersOffer()
        {
            _builder.Create(SituationType.SmugglersOffer)
                .Title("Smuggler's Offer")
                .Text("A smuggler who docked at the starport slips a case of credits across the desk and asks that customs look the other way.")
                .Requires(SituationRequirement.CityLevel(2))
                .Requires(SituationRequirement.WithFacility(PropertyUpgradeType.StarportLevel))

                .AddChoice("Take the bribe")
                .Gain(10000)
                .FollowUp(SituationType.Scandal, 3, 3, true)
                .Secret()
                .Gazette("Took a smuggler's bribe to keep customs quiet.")

                .AddChoice("Arrest the smuggler")
                .Hook()
                .Gazette("Arrested a smuggler who tried to bribe the starport.")

                .IgnoredGazette("The city ignored a smuggler's offer, and the smuggler slipped away.");
        }

        private void RefugeeShuttle()
        {
            _builder.Create(SituationType.RefugeeShuttle)
                .Title("Refugee Shuttle")
                .Text("A shuttle full of refugees asks leave to land at the starport. They have little, and nowhere else to go.")
                .Requires(SituationRequirement.CityLevel(2))
                .Requires(SituationRequirement.WithFacility(PropertyUpgradeType.StarportLevel))

                .AddChoice("Welcome them")
                .Pay(10000)
                .FollowUp(SituationType.NewArrivals, 2)
                .Hook()
                .Gazette("Welcomed a shuttle of refugees to the city.")

                .AddChoice("Turn them away")
                .Gazette("Turned a refugee shuttle away from the starport.")

                .IgnoredGazette("The city ignored a refugee shuttle, and it left without landing.");
        }

        private void NewArrivals()
        {
            _builder.Create(SituationType.NewArrivals)
                .Title("New Arrivals")
                .Text("The refugees the city welcomed need housing and work. Local landlords want to know what the city plans to do.")
                .FollowUpOnly()

                .AddChoice("Fund a settlement camp")
                .Pay(5000)
                .Gazette("Funded a camp to settle the city's new arrivals.")

                .AddChoice("Let them find their own way")
                .Gazette("Left the city's new arrivals to find their own way.")

                .IgnoredGazette("The city ignored the needs of its new arrivals.");
        }
    }
}
