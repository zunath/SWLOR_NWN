using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// The planetary council a DM last called for the city's planet (Player City Revamp, "Planetary councils"). The city's
    /// mayor and officers read it once at their next login.
    /// </summary>
    public class CityCouncilSummons
    {
        public CityCouncilSummons()
        {
            Id = string.Empty;
            Text = string.Empty;
        }

        /// <summary>Tells one summons from the next, so a login notice shows once.</summary>
        public string Id { get; set; }

        /// <summary>The planet whose council was called.</summary>
        public PlanetType Planet { get; set; }

        /// <summary>The summons as the mayor and officers read it.</summary>
        public string Text { get; set; }

        public DateTime PostedUtc { get; set; }
    }
}
