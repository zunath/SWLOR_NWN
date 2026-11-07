using System.Collections.Generic;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.IndustryService;

namespace SWLOR.Game.Server.Feature.SituationDefinition
{
    /// <summary>
    /// City Affairs situations a citizen's holding raises: a bandit raid, and a customs inspection that fined its owner. They are
    /// raised by the holding's event, never drawn at random, and run out at the event's own deadline.
    /// </summary>
    public class HoldingSituationDefinition : ISituationListDefinition
    {
        private readonly SituationBuilder _builder = new();

        public Dictionary<SituationType, SituationDetail> Build()
        {
            HoldingRaid();
            HoldingCustoms();

            return _builder.Build();
        }

        // The help costs the same as the other situations that pay to fix a problem at once (Pests, New Arrivals). The fine is paid
        // up to the 15,000 limit on a situation's cost. Ignoring either one changes nothing the holding's own deadline does not.
        private void HoldingRaid()
        {
            _builder.Create(SituationType.HoldingRaid)
                .Title("Raiders at a Holding")
                .Text("Bandits have seized a citizen's holding on the city's planet. Unless they are driven off in time, they carry off half the stock in its warehouse. The owner can fight them at the holding's site, or the city can send its guards.")
                .EventOnly()

                .AddChoice("Send the city's guards")
                .SendHelp()
                .Pay(HoldingSituationRules.RaidHelpCost)
                .Gazette("Sent the city's guards to drive raiders off a citizen's holding.")

                .AddChoice("Leave it to the owner")
                .Gazette("Left a citizen to deal with raiders at their holding.")

                .IgnoredGazette("The city ignored a bandit raid on a citizen's holding.");
        }

        private void HoldingCustoms()
        {
            _builder.Create(SituationType.HoldingCustoms)
                .Title("Customs at a Holding")
                .Text("Customs inspectors found banned stock at a citizen's holding and fined its owner. If the fine is not paid in time, they confiscate the stock. The owner can pay it, or the city can pay it from the treasury.")
                .EventOnly()

                .AddChoice("Pay the fine from the treasury")
                .PayHoldingFine()
                .Gazette("Paid a citizen's customs fine from the treasury.")

                .AddChoice("Let the owner pay")
                .Gazette("Left a citizen to pay a customs fine at their holding.")

                .IgnoredGazette("The city ignored a customs fine at a citizen's holding.");
        }
    }
}
