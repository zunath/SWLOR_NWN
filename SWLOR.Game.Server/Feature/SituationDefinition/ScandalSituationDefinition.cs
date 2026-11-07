using System.Collections.Generic;
using SWLOR.Game.Server.Service.CityAffairsService;

namespace SWLOR.Game.Server.Feature.SituationDefinition
{
    /// <summary>The follow-up that exposes a secret choice.</summary>
    public class ScandalSituationDefinition : ISituationListDefinition
    {
        private readonly SituationBuilder _builder = new();

        public Dictionary<SituationType, SituationDetail> Build()
        {
            Scandal();

            return _builder.Build();
        }

        private void Scandal()
        {
            _builder.Create(SituationType.Scandal)
                .Title("Scandal")
                .Text("Word has spread that an official took a bribe from a smuggler. The Gazette has named them, and citizens want a response.")
                .FollowUpOnly()
                .ExposesSecret()

                .AddChoice("Apologize publicly")
                .Hook()
                .Gazette("Apologized to the city for the scandal.")

                .AddChoice("Pay restitution")
                .Pay(7500)
                .Hook()
                .Gazette("Paid restitution to the city over the scandal.")

                .IgnoredHook()
                .IgnoredGazette("The city ignored a scandal, and trust in its officials has suffered.");
        }
    }
}
