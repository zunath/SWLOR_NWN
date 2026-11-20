namespace SWLOR.Game.Server.Domain.Foundation
{
    /// <summary>
    /// A time zone a player can pick in Settings. Times convert through the IANA zone, so daylight saving is applied.
    /// </summary>
    public sealed record PlayerTimeZone
    {
        /// <summary>The IANA zone id, saved on the player.</summary>
        public string Id { get; }

        /// <summary>The name shown in the Settings drop-down.</summary>
        public string Label { get; }

        /// <summary>Short name shown after times outside daylight saving, or null to show the UTC offset.</summary>
        public string StandardAbbreviation { get; }

        /// <summary>Short name shown after times during daylight saving, or null to show the UTC offset.</summary>
        public string DaylightAbbreviation { get; }

        public PlayerTimeZone(string id, string label, string standardAbbreviation = null, string daylightAbbreviation = null)
        {
            Id = id;
            Label = label;
            StandardAbbreviation = standardAbbreviation;
            DaylightAbbreviation = daylightAbbreviation ?? standardAbbreviation;
        }
    }
}
