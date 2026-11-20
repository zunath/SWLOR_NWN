using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// Chat and display preferences for a DM. DM avatars have no Player record, so these follow the DM's
    /// public CD key onto any avatar and onto creatures they possess.
    /// </summary>
    public class DMPreferences : EntityBase
    {
        public EmoteStyle EmoteStyle { get; set; }
        public DMCommsFilter CommsFilter { get; set; }
        public bool ShowHelmet { get; set; }
        public bool ShowCloak { get; set; }
        // IANA zone id from PlayerTimeZones for event times. Null means UTC.
        public string TimeZoneId { get; set; }

        public DMPreferences()
        {
            EmoteStyle = EmoteStyle.Regular;
            CommsFilter = DMCommsFilter.AllParties;
            ShowHelmet = true;
            ShowCloak = true;
        }
    }
}
