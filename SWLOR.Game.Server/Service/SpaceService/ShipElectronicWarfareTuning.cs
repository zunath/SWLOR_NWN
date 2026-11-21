namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The hull bonuses of the Ion Web and Interdictor (W11k), from the Bible's Ships (Revamp) tab: the Interceptor role
    /// (tools/SpaceRevampBalance/model.py ROLES) and the Republic Scout and Barracuda signatures (model.py SHIPS). The
    /// module numbers themselves are each step's (RevampModuleCatalog).
    /// </summary>
    public static class ShipElectronicWarfareTuning
    {
        /// <summary>The Interceptor role adds this percentage to an Ion Web's slow.</summary>
        public const int InterceptorStrengthPercent = 25;

        /// <summary>The Interceptor role adds this percentage to an Ion Web's and an Interdictor's range.</summary>
        public const int InterceptorRangePercent = 50;

        /// <summary>The Republic Scout's signature adds this percentage to its Ion Web's range.</summary>
        public const string RepublicScoutTag = "sd_republicscout";
        public const int RepublicScoutWebRangePercent = 20;

        /// <summary>The Barracuda's signature: Ion Webs do not slow it.</summary>
        public const string BarracudaTag = "sd_barracuda";

        /// <summary>The slowest an Ion Web can make a ship, in percent.</summary>
        public const int MaximumWebPercent = 75;

        /// <summary>
        /// A web, jam, paint or hold lasts this much longer than its module's cycle, so a running module keeps it up
        /// without a gap (as the Shield Hardener and the E-War Array do).
        /// </summary>
        public const double HoldPaddingSeconds = 0.5;
    }
}
